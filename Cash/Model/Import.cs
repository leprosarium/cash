using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Cash.Model
{
    namespace Import
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
