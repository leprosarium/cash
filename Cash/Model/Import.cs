using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Cash.Model
{
    namespace StaticImport
    {
        public interface Parser<T>
        {
            Task<bool> Parse(T t, object tag, XmlReader reader);
        }

        public class ListParser<T> : Parser<List<T>>
    where T : new()
        {
            object nodetag;
            Parser<T> imp;
            public ListParser(Parser<T> imp, string tag, XmlReader reader)
            {
                this.nodetag = reader.NameTable.Add(tag);
                this.imp = imp;
            }

            async public Task<bool> Parse(List<T> t, object tag, XmlReader reader)
            {
                if (tag == nodetag)
                    t.Add(await imp.Import(reader));
                else
                    return false;
                return true;
            }
        }

        public static class Importer
        {
            async public static Task<T> Import<T>(this Parser<T> imp, XmlReader reader) where T : new()
            {
                object startTag = reader.Name;
                T obj = new T();
                while (await reader.ReadAsync())
                {
                    object tag = reader.Name;
                    if (reader.NodeType == XmlNodeType.EndElement && tag == startTag)
                        break;
                    if (reader.NodeType == XmlNodeType.Element)
                        if (!await imp.Parse(obj, tag, reader))
                            await reader.SkipAsync();
                }
                return obj;
            }

            public static ListParser<T> MakeListParser<T>(this Parser<T> imp, string tag, XmlReader reader) where T : new()
            {
                return new ListParser<T>(imp, tag, reader);
            }

            async public static Task<Commodity> ReadElementContentAsCommodityAsync(this XmlReader reader)
            {
                return await new CommodityParser(reader).Import(reader);
            }
        }

        public partial class Commodity
        {
            public string Space { get; set; }
            public string ID { get; set; }
            public Commodity()
            {
                Space = "";
                ID = "";
            }
        }

        public class Account
        {
            public string Name { get; set; }
            public string ID { get; set; }
            public string Type { get; set; }
            public string Parent { get; set; }
            public Commodity Comm { get; set; }
            public Dictionary<string, object> Slots { get; set; }

            public Account()
            {
                Name = "";
                ID = "";
                Type = "";
                Parent = "";
                Comm = new Commodity();
                Slots = new Dictionary<string, object>();
            }
        }

        public class Slot
        {
            public string Key { get; set; }
            public object Value { get; set; }

            public Slot()
            {
                Key = "";
                Value = null;
            }

        }

        public class Split
        {
            public string ID { get; set; }
            public string ReconciledState { get; set; }
            public DateTime ReconcileDate { get; set; }
            public decimal Value { get; set; }
            public decimal Quantity { get; set; }
            public string Account { get; set; }
            public Split()
            {
                ID = "";
                ReconciledState = "";
                ReconcileDate = new DateTime();
                Value = 0;
                Quantity = 0;
                Account = "";
            }
        }

        public class Transaction
        {
            public string ID { get; set; }
            public Commodity Comm { get; set; }
            public DateTime Posted { get; set; }
            public DateTime Entered { get; set; }
            public string Description { get; set; }
            public Dictionary<string, object> Slots { get; set; }
            public List<Split> Splits { get; set; }
            public Transaction()
            {
                ID = "";
                Comm = new Commodity();
                Posted = new DateTime();
                Entered = new DateTime();
                Description = "";
                Slots = new Dictionary<string, object>();
                Splits = new List<Split>();
            }
        }

        public class Book
        {
            public string ID { get; set; }
            public List<Account> Accounts { get; set; }
            public List<Transaction> Transactions { get; set; }
            public Dictionary<string, int> Counts { get; set; }
            public Book()
            {
                ID = "";
                Accounts = new List<Account>();
                Transactions = new List<Transaction>();
                Counts = new Dictionary<string, int>();
            }
        }

        public class CommodityParser : Parser<Commodity>
        {
            object space, id;
            public CommodityParser(XmlReader reader)
            {
                space = reader.NameTable.Add("cmdty:space");
                id = reader.NameTable.Add("cmdty:id");
            }
            public Task<bool> Parse(Commodity c, object tag, XmlReader reader)
            {
                if (tag == space)
                    c.Space = reader.ReadElementContentAsString();
                else if (tag == id)
                    c.ID = reader.ReadElementContentAsString();
                else
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
        }


        public class AccountParser : Parser<Account>
        {
            object name, id, type, parent, commodity, slots;
            CommodityParser  commodityParser;
            SlotsParser slotsParser;

            public AccountParser(XmlReader reader)
            {
                name = reader.NameTable.Add("act:name");
                id = reader.NameTable.Add("act:id");
                type = reader.NameTable.Add("act:type");
                parent = reader.NameTable.Add("act:parent");
                commodity = reader.NameTable.Add("act:commodity");
                slots = reader.NameTable.Add("act:slots");
                commodityParser = new CommodityParser(reader);
                slotsParser = new SlotsParser(reader);
            }

            async public Task<bool> Parse(Account obj, object tag, XmlReader reader)
            {
                if (tag == name)
                    obj.Name = reader.ReadElementContentAsString();
                else if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == type)
                    obj.Type = reader.ReadElementContentAsString();
                else if (tag == parent)
                    obj.Parent = reader.ReadElementContentAsString();
                else if (tag == commodity)
                    obj.Comm = await commodityParser.Import(reader);
                else if (tag == slots)
                    obj.Slots = await slotsParser.Import(reader);
                else
                    return false;
                return true;
            }
        }

        public class SlotParser : Parser<Slot>
        {
            object key, value;
            SlotsParser frame;
            public SlotParser(SlotsParser frame, XmlReader reader)
            {
                key = reader.NameTable.Add("slot:key");
                value = reader.NameTable.Add("slot:value");
                this.frame = frame;
            }

            async public Task<bool> Parse(Slot obj, object tag, XmlReader reader)
            {
                if (tag == key)
                    obj.Key = reader.ReadElementContentAsString();
                else if (tag == value)
                    obj.Value = await ImportSlotValue(reader);
                else
                    return false;
                return true;
            }

            async private Task<object> ImportSlotValue(XmlReader reader)
            {
                switch (reader.GetAttribute("type"))
                {
                    case "string":
                        return reader.ReadElementContentAsString();
                    case "integer":
                        return reader.ReadElementContentAsInt();
                    case "frame":
                        return await frame.Import(reader);
                }
                return null;
            }

        }


        public class SlotsParser : Parser<Dictionary<string, object>>
        {
            object slot;
            SlotParser slotParser;
            public SlotsParser(XmlReader reader)
            {
                slot = reader.NameTable.Add("slot");
                slotParser = new SlotParser(this, reader);
            }
            async public Task<bool> Parse(Dictionary<string, object> obj, object tag, XmlReader reader)
            {
                if (tag == slot)
                {
                    Slot sl = await slotParser.Import(reader);
                    obj.Add(sl.Key, sl.Value);
                }
                else
                    return false;
                return true;
            }
        }

        public class DateTimeParser : Parser<DateTime>
        {
            object date;
            public DateTimeParser(XmlReader reader)
            {
                date = reader.NameTable.Add("ts:date");
            }
            public Task<bool> Parse(DateTime obj, object tag, XmlReader reader)
            {
                if (tag == date)
                    obj = DateTime.ParseExact(reader.ReadElementContentAsString(), "yyyy-MM-dd HH:mm:ss zzz", null);
                else
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
        }
        public class SplitParser : Parser<Split>
        {
            object id, reconciledstate, reconciledate, value, quantity, account;
            DateTimeParser dtParser;
            public SplitParser(XmlReader reader)
            {
                id = reader.NameTable.Add("split:id");
                reconciledstate = reader.NameTable.Add("split:reconciled-state");
                reconciledate = reader.NameTable.Add("split:reconcile-date");
                value = reader.NameTable.Add("split:value");
                quantity = reader.NameTable.Add("split:quantity");
                account = reader.NameTable.Add("split:account");
                dtParser = new DateTimeParser(reader);
            }

            async public Task<bool> Parse(Split obj, object tag, XmlReader reader)
            {
                if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == reconciledstate)
                    obj.ReconciledState = reader.ReadElementContentAsString();
                else if (tag == reconciledate)
                    obj.ReconcileDate = await dtParser.Import(reader);
                else if (tag == value)
                    obj.Value = ParseDecimal(reader);
                else if (tag == quantity)
                    obj.Quantity = ParseDecimal(reader);
                else if (tag == account)
                    obj.Account = reader.ReadElementContentAsString();
                else
                    return false;
                return true;
            }

            private static Decimal ParseDecimal(XmlReader reader)
            {
                string str = reader.ReadElementContentAsString();
                int div = str.IndexOf('/');
                if (div == -1)
                    return Decimal.Parse(str);
                return Decimal.Parse(str.Substring(0, div)) / Int32.Parse(str.Substring(div + 1));
            }
        }

        public class TransactionParser : Parser<Transaction>
        {
            object id, curr, posted, entered, descr, slots, splits;
            CommodityParser commodity;
            SlotsParser slotsParser;
            DateTimeParser dtParser;
            Parser<List<Split>> splitsParser;
            public TransactionParser(XmlReader reader)
            {
                id = reader.NameTable.Add("trn:id");
                curr = reader.NameTable.Add("trn:currency");
                posted = reader.NameTable.Add("trn:date-posted");
                entered = reader.NameTable.Add("trn:date-entered");
                descr = reader.NameTable.Add("trn:description");
                slots = reader.NameTable.Add("trn:slots");
                splits = reader.NameTable.Add("trn:splits");

                commodity = new CommodityParser(reader);
                slotsParser = new SlotsParser(reader);
                dtParser = new DateTimeParser(reader);
                var splitParser = new SplitParser(reader);
                splitsParser = splitParser.MakeListParser("trn:split", reader);
            }

            async public Task<bool> Parse(Transaction obj, object tag, XmlReader reader)
            {
                if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == curr)
                    obj.Comm = await commodity.Import(reader);
                else if (tag == posted)
                    obj.Posted = await dtParser.Import(reader);
                else if (tag == entered)
                    obj.Entered = await dtParser.Import(reader);
                else if (tag == descr)
                    obj.Description = reader.ReadElementContentAsString();
                else if (tag == slots)
                    obj.Slots = await slotsParser.Import(reader);
                else if (tag == splits)
                    obj.Splits = await splitsParser.Import(reader);
                else
                    return false;
                return true;
            }
        }

        public class BookParser : Parser<Book>
        {
            object id, account, transaction, countdata;
            AccountParser accounts;
            TransactionParser transactions;
            public BookParser(XmlReader reader)
            {
                id = reader.NameTable.Add("book:id");
                account = reader.NameTable.Add("gnc:account");
                transaction = reader.NameTable.Add("gnc:transaction");
                countdata = reader.NameTable.Add("gnc:count-data");
                accounts = new AccountParser(reader);
                transactions = new TransactionParser(reader);
            }

            async public Task<bool> Parse(Book obj, object tag, XmlReader reader)
            {
                if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == account)
                    obj.Accounts.Add(await accounts.Import(reader));
                else if (tag == transaction)
                    obj.Transactions.Add(await transactions.Import(reader));
                else if (tag == countdata)
                {
                    string type = reader.GetAttribute("cd:type");
                    int count = reader.ReadElementContentAsInt();
                    obj.Counts.Add(type, count);
                }
                else
                    return false;
                return true;
            }
        }
    }

    namespace Import
    {
        public abstract class Importer<T> where T : new()
        {
            protected XmlReader reader;
            protected abstract Task<bool> ParseNode(T obj, object tag);
            protected object Register(string str) { return reader.NameTable.Add(str); }
            protected Importer(XmlReader reader)
            {
                this.reader = reader;
            }
            async public Task<T> Import()
            {
                object startTag = reader.Name;
                T obj = new T();
                while (await reader.ReadAsync())
                {
                    object tag = reader.Name;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (! await ParseNode(obj, tag))
                            await reader.SkipAsync();
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && tag == startTag)
                        break;
                }
                return obj;
            }
        }
        
        public class Commodity
        {
            public class Importer : Importer<Commodity>
            {
                object space, id;
                public Importer(XmlReader reader) : base(reader)
                {
                    space = Register("cmdty:space");
                    id = Register("cmdty:id");
                }
                protected override Task<bool> ParseNode(Commodity obj, object tag)
                {
                    if (tag == space)
                        obj.Space = reader.ReadElementContentAsString();
                    else if (tag == id)
                        obj.ID = reader.ReadElementContentAsString();
                    else
                        return Task.FromResult(false);
                    return Task.FromResult(true);
                }
            }

            public string Space { get; set; }
            public string ID { get; set; }

            public Commodity()
            {
                Space = "";
                ID = "";
            }
        }

        public class Account
        {
            public class Importer : Importer<Account>
            {
                object name, id, type, parent, commodity, slots;
                Commodity.Importer commodityimp;
                SlotsImporter slotsimp;

                public Importer(XmlReader reader) : base(reader)
                {
                    name = Register("act:name");
                    id = Register("act:id");
                    type = Register("act:type");
                    parent = Register("act:parent");
                    commodity = Register("act:commodity");
                    slots = Register("act:slots");
                    commodityimp = new Commodity.Importer(reader);
                    slotsimp = new SlotsImporter(reader);
                }

                async protected override Task<bool> ParseNode(Account obj, object tag)
                {
                    if (tag == name)
                        obj.Name = reader.ReadElementContentAsString();
                    else if (tag == id)
                        obj.ID = reader.ReadElementContentAsString();
                    else if (tag == type)
                        obj.Type = reader.ReadElementContentAsString();
                    else if (tag == parent)
                        obj.Parent = reader.ReadElementContentAsString();
                    else if (tag == commodity)
                        obj.Comm = await commodityimp.Import();
                    else if (tag == slots)
                        obj.Slots = await slotsimp.Import();
                    else
                        return false;
                    return true;
                }
            }
            
            public string Name { get; set; }
            public string ID { get; set; }
            public string Type { get; set; }
            public string Parent { get; set; }
            public Commodity Comm { get; set; }
            public Dictionary<string, object> Slots { get; set; }

            public Account()
            {
                Name = "";
                ID = "";
                Type = "";
                Parent = "";
                Comm = new Commodity();
                Slots = new Dictionary<string, object>();
            }
        }

        public class Slot
        {
            public string Key { get; set; }
            public object Value { get; set; }

            public Slot()
            {
                Key = "";
                Value = null;
            }

            public class Importer : Importer<Slot>
            {
                object key, value;
                SlotsImporter frame;
                public Importer(SlotsImporter frame, XmlReader reader) : base(reader)
                {
                    key = Register("slot:key");
                    value = Register("slot:value");
                    this.frame = frame;
                }

                async protected override Task<bool> ParseNode(Slot obj, object tag)
                {
                    if (tag == key)
                        obj.Key = reader.ReadElementContentAsString();
                    else if (tag == value)
                        obj.Value = await ImportSlotValue();
                    else
                        return false;
                    return true;
                }
                async private Task<object> ImportSlotValue()
                {
                    switch (reader.GetAttribute("type"))
                    {
                        case "string":
                            return reader.ReadElementContentAsString();
                        case "integer":
                            return reader.ReadElementContentAsInt();
                        case "frame":
                            return await frame.Import();
                    }
                    return null;
                }

            }
        }

        public class SlotsImporter : Importer<Dictionary<string, object>>
        {
            object slot;
            Slot.Importer slotimp;
            public SlotsImporter(XmlReader reader)
                : base(reader)
            {
                slot = Register("slot");
                slotimp = new Slot.Importer(this, reader);
            }

            async protected override Task<bool> ParseNode(Dictionary<string, object> obj, object tag)
            {
                if (tag == slot)
                {
                    Slot sl = await slotimp.Import();
                    obj.Add(sl.Key, sl.Value);
                }
                else
                    return false;
                return true;
            }
        }

        public class DateTimeImporter : Importer<DateTime>
        {
            object date;
            public DateTimeImporter(XmlReader reader) : base(reader)
            {
                date = Register("ts:date");
            }
            protected override Task<bool> ParseNode(DateTime obj, object tag)
            {
                if (tag == date)
                    obj = DateTime.ParseExact(reader.ReadElementContentAsString(), "yyyy-MM-dd HH:mm:ss zzz", null);
                else
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
        }

        public class Split
        {
            public string ID { get; set; }
            public string ReconciledState { get; set; }
            public DateTime ReconcileDate { get; set; }
            public decimal Value { get; set; }
            public decimal Quantity { get; set; }
            public string Account { get; set; }
            public Split()
            {
                ID = "";
                ReconciledState = "";
                ReconcileDate = new DateTime();
                Value = 0;
                Quantity = 0;
                Account = "";
            }

            public class Importer : Importer<Split>
            {
                object id, reconciledstate, reconciledate, value, quantity, account;
                DateTimeImporter dtimp;
                public Importer(XmlReader reader) : base(reader)
                {
                    id = Register("split:id");
                    reconciledstate = Register("split:reconciled-state");
                    reconciledate = Register("split:reconcile-date");
                    value = Register("split:value");
                    quantity = Register("split:quantity");
                    account = Register("split:account");
                    dtimp = new DateTimeImporter(reader);
                }

                async protected override Task<bool> ParseNode(Split obj, object tag)
                {
                    if (tag == id)
                        obj.ID = reader.ReadElementContentAsString();
                    else if (tag == reconciledstate)
                        obj.ReconciledState = reader.ReadElementContentAsString();
                    else if (tag == reconciledate)
                        obj.ReconcileDate = await dtimp.Import();
                    else if (tag == value)
                        obj.Value = ParseDecimal();
                    else if (tag == quantity)
                        obj.Quantity = ParseDecimal();
                    else if (tag == account)
                        obj.Account = reader.ReadElementContentAsString();
                    else
                        return false;
                    return true;
                }

                private Decimal ParseDecimal()
                {
                    string str = reader.ReadElementContentAsString();
                    int div = str.IndexOf('/');
                    if (div == -1)
                        return Decimal.Parse(str);
                    return Decimal.Parse(str.Substring(0, div)) / Int32.Parse(str.Substring(div + 1));
                }
            }
        }


        public class SplitsImporter : Importer<List<Split>>
        {
            object split;
            Split.Importer splitimp;
            public SplitsImporter(XmlReader reader)
                : base(reader)
            {
                split = Register("trn:split");
                splitimp = new Split.Importer(reader);
            }

            async protected override Task<bool> ParseNode(List<Split> obj, object tag)
            {
                if (tag == split)
                    obj.Add(await splitimp.Import());
                else
                    return false;
                return true;
            }
        }

        public class Transaction
        {
            public class Importer : Importer<Transaction>
            {
                object id, curr, posted, entered, descr, slots, splits;
                Commodity.Importer commodity;
                SlotsImporter slotsimp;
                DateTimeImporter dtimp;
                SplitsImporter splitsimp;
                public Importer(XmlReader reader)
                    : base(reader)
                {
                    id = Register("trn:id");
                    curr = Register("trn:currency");
                    posted = Register("trn:date-posted");
                    entered = Register("trn:date-entered");
                    descr = Register("trn:description");
                    slots = Register("trn:slots");
                    splits = Register("trn:splits");
                  
                    commodity = new Commodity.Importer(reader);
                    slotsimp = new SlotsImporter(reader);
                    dtimp = new DateTimeImporter(reader);
                    splitsimp = new SplitsImporter(reader);
                }

                async protected override Task<bool> ParseNode(Transaction obj, object tag)
                {
                    if (tag == id)
                        obj.ID = reader.ReadElementContentAsString();
                    else if (tag == curr)
                        obj.Comm = await commodity.Import();
                    else if (tag == posted)
                        obj.Posted = await dtimp.Import();
                    else if (tag == entered)
                        obj.Entered = await dtimp.Import();
                    else if (tag == descr)
                        obj.Description = reader.ReadElementContentAsString();
                    else if (tag == slots)
                        obj.Slots = await slotsimp.Import();
                    else if (tag == splits)
                        obj.Splits = await splitsimp.Import();
                    else
                        return false;
                    return true;
                }
            }
            public string ID { get; set; }
            public Commodity Comm { get; set; }
            public DateTime Posted { get; set; }
            public DateTime Entered { get; set; }
            public string Description { get; set; }
            public Dictionary<string, object> Slots { get; set; }
            public List<Split> Splits { get; set; }
            public Transaction()
            {
                ID = "";
                Comm = new Commodity();
                Posted = new DateTime();
                Entered = new DateTime();
                Description = "";
                Slots = new Dictionary<string, object>();
                Splits = new List<Split>();
            }
        }

        public class Book
        {
            public string ID { get; set; }
            public List<Account> Accounts { get; set; }
            public List<Transaction> Transactions { get; set; }
            public Dictionary<string, int> Counts { get; set; }
            public Book()
            {
                ID = "";
                Accounts = new List<Account>();
                Transactions = new List<Transaction>();
                Counts = new Dictionary<string, int>();
            }
            public class Importer : Importer<Book>
            {
                object id, account, transaction, countdata;
                Account.Importer accounts;
                Transaction.Importer transactions;
                public Importer(XmlReader reader)
                    : base(reader)
                {
                    id = Register("book:id");
                    account = Register("gnc:account");
                    transaction = Register("gnc:transaction");
                    countdata = Register("gnc:count-data");
                    accounts = new Account.Importer(reader);
                    transactions = new Transaction.Importer(reader);
                }

                async protected override Task<bool> ParseNode(Book obj, object tag)
                {
                    if (tag == id)
                        obj.ID = reader.ReadElementContentAsString();
                    else if (tag == account)
                        obj.Accounts.Add(await accounts.Import());
                    else if (tag == transaction)
                        obj.Transactions.Add(await transactions.Import());
                    else if (tag == countdata)
                    {
                        string type = reader.GetAttribute("cd:type");
                        int count = reader.ReadElementContentAsInt();
                        obj.Counts.Add(type, count);
                    }
                    else
                        return false;
                    return true;
                }
            }
        }

            public class GNCImporter : Importer<List<Book>>
            {
                object book;
                Book.Importer books;
                public GNCImporter(XmlReader reader)
                    : base(reader)
                {
                    book = Register("gnc:book");
                    books = new Book.Importer(reader);
                }

                async protected override Task<bool> ParseNode(List<Book> obj, object tag)
                {
                    if (tag == book)
                        obj.Add(await books.Import());
                    else
                        return false;
                    return true;
                }
            }
    }

    namespace Import2
    {
        public interface Reader<T>
        {
            void Register(XmlReader reader);
            Task<bool> Read(T t, object tag, XmlReader reader);
        }

        public class ListReader<T> : Reader<List<T>>
            where T : new()
        {
            object nodeTag;
            Reader<T> nodeReader;

            public void Register(XmlReader reader)
            {
                nodeTag = reader.Register((string)nodeTag);
                reader.Register(nodeReader);
            }

            public ListReader(Reader<T> nodeReader, string nodeTag)
            {
                this.nodeTag = nodeTag;
                this.nodeReader = nodeReader;
            }

            async public Task<bool> Read(List<T> t, object tag, XmlReader reader)
            {
                if (tag == nodeTag)
                    t.Add(await reader.ReadAsyncBy(nodeReader));
                else
                    return false;
                return true;
            }
        }

        public static class Importer
        {
            async public static Task<T> ReadAsyncBy<T>(this XmlReader reader, Reader<T> imp) where T : new()
            {
                object startTag = reader.Name;
                T obj = new T();
                while (await reader.ReadAsync())
                {
                    object tag = reader.Name;
                    if (reader.NodeType == XmlNodeType.EndElement && tag == startTag)
                        break;
                    if (reader.NodeType == XmlNodeType.Element)
                        if (!await imp.Read(obj, tag, reader))
                            await reader.SkipAsync();
                }
                return obj;
            }

            public static string Register(this XmlReader reader, string tag)
            {
                return reader.NameTable.Add(tag);
            }

            public static Reader<T> Register<T>(this XmlReader reader, Reader<T> p)
            {
                p.Register(reader);
                return p;
            }

            public static ListReader<T> AsListReader<T>(this Reader<T> imp, string tag) where T : new()
            {
                return new ListReader<T>(imp, tag);
            }

        }

        public partial class Commodity
        {
            public string Space { get; set; }
            public string ID { get; set; }

            public Commodity()
            {
                Space = "";
                ID = "";
            }
        }

        public class Account
        {
            public string Name { get; set; }
            public string ID { get; set; }
            public string Type { get; set; }
            public string Parent { get; set; }
            public Commodity Comm { get; set; }
            public Dictionary<string, object> Slots { get; set; }

            public Account()
            {
                Name = "";
                ID = "";
                Type = "";
                Parent = "";
                Comm = new Commodity();
                Slots = new Dictionary<string, object>();
            }
        }

        public class Slot
        {
            public string Key { get; set; }
            public object Value { get; set; }

            public Slot()
            {
                Key = "";
                Value = null;
            }

        }

        public class Split
        {
            public string ID { get; set; }
            public string ReconciledState { get; set; }
            public DateTime ReconcileDate { get; set; }
            public decimal Value { get; set; }
            public decimal Quantity { get; set; }
            public string Account { get; set; }

            public Split()
            {
                ID = "";
                ReconciledState = "";
                ReconcileDate = new DateTime();
                Value = 0;
                Quantity = 0;
                Account = "";
            }
        }

        public class Transaction
        {
            public string ID { get; set; }
            public Commodity Comm { get; set; }
            public DateTime Posted { get; set; }
            public DateTime Entered { get; set; }
            public string Description { get; set; }
            public Dictionary<string, object> Slots { get; set; }
            public List<Split> Splits { get; set; }

            public Transaction()
            {
                ID = "";
                Comm = new Commodity();
                Posted = new DateTime();
                Entered = new DateTime();
                Description = "";
                Slots = new Dictionary<string, object>();
                Splits = new List<Split>();
            }
        }

        public class Book
        {
            public string ID { get; set; }
            public List<Account> Accounts { get; set; }
            public List<Transaction> Transactions { get; set; }
            public Dictionary<string, int> Counts { get; set; }

            public Book()
            {
                ID = "";
                Accounts = new List<Account>();
                Transactions = new List<Transaction>();
                Counts = new Dictionary<string, int>();
            }
        }

        public class CommodityReader : Reader<Commodity>
        {
            object space, id;

            public void Register(XmlReader reader)
            {
                space = reader.Register("cmdty:space");
                id = reader.Register("cmdty:id");
            }

            public Task<bool> Read(Commodity c, object tag, XmlReader reader)
            {
                if (tag == space)
                    c.Space = reader.ReadElementContentAsString();
                else if (tag == id)
                    c.ID = reader.ReadElementContentAsString();
                else
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
        }

        public class AccountReader : Reader<Account>
        {
            object name, id, type, parent, commodity, slots;
            CommodityReader commodityReader = new CommodityReader();
            SlotsReader slotsReader = new SlotsReader();

            public void Register(XmlReader reader)
            {
                name = reader.Register("act:name");
                id = reader.Register("act:id");
                type = reader.Register("act:type");
                parent = reader.Register("act:parent");
                commodity = reader.Register("act:commodity");
                slots = reader.Register("act:slots");
                reader.Register(commodityReader);
                reader.Register(slotsReader);
            }

            async public Task<bool> Read(Account obj, object tag, XmlReader reader)
            {
                if (tag == name)
                    obj.Name = reader.ReadElementContentAsString();
                else if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == type)
                    obj.Type = reader.ReadElementContentAsString();
                else if (tag == parent)
                    obj.Parent = reader.ReadElementContentAsString();
                else if (tag == commodity)
                    obj.Comm = await reader.ReadAsyncBy(commodityReader);
                else if (tag == slots)
                    obj.Slots = await reader.ReadAsyncBy(slotsReader);
                else
                    return false;
                return true;
            }
        }

        public class SlotReader : Reader<Slot>
        {
            object key, value;
            SlotsReader frameReader;

            public void Register(XmlReader reader)
            {
                key = reader.NameTable.Add("slot:key");
                value = reader.NameTable.Add("slot:value");
            }

            public SlotReader(SlotsReader frame)
            {
                this.frameReader = frame;
            }

            async public Task<bool> Read(Slot obj, object tag, XmlReader reader)
            {
                if (tag == key)
                    obj.Key = reader.ReadElementContentAsString();
                else if (tag == value)
                    obj.Value = await ReadSlotValue(reader);
                else
                    return false;
                return true;
            }

            async private Task<object> ReadSlotValue(XmlReader reader)
            {
                switch (reader.GetAttribute("type"))
                {
                    case "string":
                        return reader.ReadElementContentAsString();
                    case "integer":
                        return reader.ReadElementContentAsInt();
                    case "frame":
                        return await reader.ReadAsyncBy(frameReader);
                }
                return null;
            }
        }

        public class SlotsReader : Reader<Dictionary<string, object>>
        {
            object slot;
            SlotReader slotReader;

            public void Register(XmlReader reader)
            {
                slot = reader.Register("slot");
                reader.Register(slotReader);
            }

            public SlotsReader()
            {
                slotReader = new SlotReader(this);
            }

            async public Task<bool> Read(Dictionary<string, object> obj, object tag, XmlReader reader)
            {
                if (tag == slot)
                {
                    Slot sl = await reader.ReadAsyncBy(slotReader);
                    obj.Add(sl.Key, sl.Value);
                }
                else
                    return false;
                return true;
            }
        }

        public class DateTimeReader : Reader<DateTime>
        {
            object date;

            public void Register(XmlReader reader)
            {
                date = reader.Register("ts:date");
            }

            public Task<bool> Read(DateTime obj, object tag, XmlReader reader)
            {
                if (tag == date)
                    obj = DateTime.ParseExact(reader.ReadElementContentAsString(), "yyyy-MM-dd HH:mm:ss zzz", null);
                else
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
        }

        public class SplitReader : Reader<Split>
        {
            object id, reconciledstate, reconciledate, value, quantity, account;
            DateTimeReader dtReader = new DateTimeReader();

            public void Register(XmlReader reader)
            {
                id = reader.Register("split:id");
                reconciledstate = reader.Register("split:reconciled-state");
                reconciledate = reader.Register("split:reconcile-date");
                value = reader.Register("split:value");
                quantity = reader.Register("split:quantity");
                account = reader.Register("split:account");
                reader.Register(dtReader);
            }
            
            async public Task<bool> Read(Split obj, object tag, XmlReader reader)
            {
                if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == reconciledstate)
                    obj.ReconciledState = reader.ReadElementContentAsString();
                else if (tag == reconciledate)
                    obj.ReconcileDate = await reader.ReadAsyncBy(dtReader);
                else if (tag == value)
                    obj.Value = ReadDecimal(reader);
                else if (tag == quantity)
                    obj.Quantity = ReadDecimal(reader);
                else if (tag == account)
                    obj.Account = reader.ReadElementContentAsString();
                else
                    return false;
                return true;
            }

            private static Decimal ReadDecimal(XmlReader reader)
            {
                string str = reader.ReadElementContentAsString();
                int div = str.IndexOf('/');
                if (div == -1)
                    return Decimal.Parse(str);
                return Decimal.Parse(str.Substring(0, div)) / Int32.Parse(str.Substring(div + 1));
            }
        }

        public class TransactionReader : Reader<Transaction>
        {
            object id, curr, posted, entered, descr, slots, splits;
            CommodityReader commodityReader = new CommodityReader();
            SlotsReader slotsReader = new SlotsReader();
            DateTimeReader dtReader = new DateTimeReader();
            Reader<List<Split>> splitsReader = new SplitReader().AsListReader("trn:split");

            public void Register(XmlReader reader)
            {
                id = reader.Register("trn:id");
                curr = reader.Register("trn:currency");
                posted = reader.Register("trn:date-posted");
                entered = reader.Register("trn:date-entered");
                descr = reader.Register("trn:description");
                slots = reader.Register("trn:slots");
                splits = reader.Register("trn:splits");

                reader.Register(commodityReader);
                reader.Register(slotsReader);
                reader.Register(dtReader);
                reader.Register(splitsReader);
            }

            async public Task<bool> Read(Transaction obj, object tag, XmlReader reader)
            {
                if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == curr)
                    obj.Comm = await reader.ReadAsyncBy(commodityReader);
                else if (tag == posted)
                    obj.Posted = await reader.ReadAsyncBy(dtReader);
                else if (tag == entered)
                    obj.Entered = await reader.ReadAsyncBy(dtReader);
                else if (tag == descr)
                    obj.Description = reader.ReadElementContentAsString();
                else if (tag == slots)
                    obj.Slots = await reader.ReadAsyncBy(slotsReader);
                else if (tag == splits)
                    obj.Splits = await reader.ReadAsyncBy(splitsReader);
                else
                    return false;
                return true;
            }
        }

        public class ProgressEventArgs : EventArgs
        {
            public double Maximum { get; set; }
            public double Value { get; set; }
        }

        public class BookReader : Reader<Book>
        {
            object id, account, transaction, countdata;
            AccountReader accountReader = new AccountReader();
            TransactionReader transactionReader = new TransactionReader();

            double maximum, value;
            long lastTime;
            public event EventHandler<ProgressEventArgs> Progress;

            public BookReader()
            {
                maximum = 0;
                value = 0;
                lastTime = Stopwatch.GetTimestamp();
            }

            private void OnProgress()
            {
                value++;
                var handler = Progress;
                if (handler != null)
                {
                    var curTime = Stopwatch.GetTimestamp();
                    if ((curTime - lastTime) * 2 > Stopwatch.Frequency)
                    {
                        lastTime = curTime;
                        handler(this, new ProgressEventArgs { Maximum = maximum, Value = value });
                    }
                }
            }

            public void Register(XmlReader reader)
            {
                id = reader.Register("book:id");
                account = reader.Register("gnc:account");
                transaction = reader.Register("gnc:transaction");
                countdata = reader.Register("gnc:count-data");
                reader.Register(accountReader);
                reader.Register(transactionReader);
            }

            async public Task<bool> Read(Book obj, object tag, XmlReader reader)
            {
                if (tag == id)
                    obj.ID = reader.ReadElementContentAsString();
                else if (tag == account)
                {
                    obj.Accounts.Add(await reader.ReadAsyncBy(accountReader));
                    OnProgress();
                }
                else if (tag == transaction)
                {
                    obj.Transactions.Add(await reader.ReadAsyncBy(transactionReader));
                    OnProgress();
                }
                else if (tag == countdata)
                {
                    string type = reader.GetAttribute("cd:type");
                    int count = reader.ReadElementContentAsInt();
                    obj.Counts.Add(type, count);
                    if (type == "account" || type == "transaction")
                        maximum += count;
                }
                else
                    return false;
                return true;
            }
        }
        static class GnuCash
        {
            static public Cash.Model.Account MakeAccountTree(List<Account> imp)
            {
                GnuCashTags tags = new GnuCashTags();
                Dictionary<string, List<Cash.Model.Account>> accounts = new Dictionary<string, List<Model.Account>>();
                Cash.Model.Account root = null;
                foreach(var acc in imp)
                {
                    var a = new Cash.Model.Account(tags.Find(acc.Type, AccountType.Passive), acc.Name, new Currency(acc.Comm.ID), acc.ID);
                    if(acc.Parent == "")
                    {
                        root = a;
                        continue;
                    }
                    List<Cash.Model.Account> l;
                    if(!accounts.TryGetValue(acc.Parent, out l))
                    {
                        l = new List<Cash.Model.Account>();
                        accounts.Add(acc.Parent, l);
                    }
                    l.Add(a);
                }
                MakeAccount(accounts, ref root);
                return root;
            }

            static void MakeAccount(Dictionary<string, List<Cash.Model.Account>> dict, ref Cash.Model.Account root)
            {
                List<Cash.Model.Account> ch;
                if (!dict.TryGetValue(root.ID, out ch))
                    return;
                root.SubAccounts = ch;

                foreach (var c in ch)
                {
                    Cash.Model.Account cc = c;
                    cc.Parent = root;
                    MakeAccount(dict, ref cc);
                }
            }
        }
    }
}
