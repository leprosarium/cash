
using System.Collections.Generic;
namespace Cash.Model
{
    public enum AccountType 
    { 
        Active, 
        Passive 
    }
    public class AccountTag
    {
        private AccountType type;
        private string tag;
        public AccountType Type { get { return type; } }
        public string Tag { get {return tag;} }
        public string Name { get; set; }
        public AccountTag(AccountType type, string tag)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            this.type = type;
            this.tag = tag;
            this.Name = loader.GetString("AccountTag/" + tag);
        }
    }

    public class AccountTags
    {
        private List<AccountTag> types;
        public AccountTags()
        {
            types = new List<AccountTag>();
            types.Add(new AccountTag(AccountType.Active, "Bank"));
            types.Add(new AccountTag(AccountType.Active, "Cash"));
            types.Add(new AccountTag(AccountType.Passive, "Credit"));
            types.Add(new AccountTag(AccountType.Active, "Asset"));
            types.Add(new AccountTag(AccountType.Passive, "Liability"));
            types.Add(new AccountTag(AccountType.Active, "Stock"));
            types.Add(new AccountTag(AccountType.Active, "Mutual"));
//            types.Add(new DefinedType(AccountType.Passive, "Currency"));
            types.Add(new AccountTag(AccountType.Passive, "Income"));
            types.Add(new AccountTag(AccountType.Active, "Expense"));
            types.Add(new AccountTag(AccountType.Passive, "Equity"));
            types.Add(new AccountTag(AccountType.Active, "Receivable"));
            types.Add(new AccountTag(AccountType.Passive, "Payable"));
//            types.Add(new DefinedType(AccountType.Passive, "Root"));
            types.Add(new AccountTag(AccountType.Passive, "Trading"));
        }
        public List<AccountTag> Types { get { return types; } }

    }

    public class GnuCashTags
    {
        private Dictionary<string, AccountTag> tags;
        public GnuCashTags()
        {
            tags = new Dictionary<string, AccountTag>();

            Add(AccountType.Active, "Bank");
            Add(AccountType.Active, "Cash");
            Add(AccountType.Passive, "Credit");
            Add(AccountType.Active, "Asset");
            Add(AccountType.Passive, "Liability");
            Add(AccountType.Active, "Stock");
            Add(AccountType.Active, "Mutual");
            Add(AccountType.Passive, "Currency");
            Add(AccountType.Passive, "Income");
            Add(AccountType.Active, "Expense");
            Add(AccountType.Passive, "Equity");
            Add(AccountType.Active, "Receivable");
            Add(AccountType.Passive, "Payable");
            Add(AccountType.Passive, "Root");
            Add(AccountType.Passive, "Trading");
        }

        private void Add(AccountType type, string tag)
        {
            tags.Add(tag.ToUpper(), new AccountTag(type, tag));
        }

        public AccountType Find(string tag, AccountType def)
        {
            AccountTag tg;
            return tags.TryGetValue(tag.ToUpper(), out tg) ? tg.Type : def;
        }
    }


}
