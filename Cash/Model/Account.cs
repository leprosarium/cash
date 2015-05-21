using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cash.Model
{
    public class Account
    {
        static Random rnd = new Random();
        private AccountType type;
        private string id;
        public AccountType Type { get { return type; } }

        public string ID { get { return id; } }
        public Currency Commodity { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Account> SubAccounts { get; set; }
        public Account Parent { get; set; }

        public Account(AccountType type, string name, Currency commodity, string id)
        {
            this.type = type;
            this.id = id;
            this.Name = name;
            this.Commodity = commodity;
            this.SubAccounts = new List<Account>();
        }

        public Account(AccountType type, string name) : this(type, name, Currency.Default, GenerateID())
        {
        }

        private static string GenerateID()
        {
            throw new NotImplementedException();
        }

        public string Path(string delim)
        { 
            if(Parent != null)
                return Parent.Path(delim) + delim + Name;
            return Name;
        }
        public void FlatList(string prefix, string delim, ref List<string> l)
        {
            foreach (var sub in SubAccounts)
            {
                string path = prefix + sub.Name;
                l.Add(path);
                sub.FlatList(path + delim, delim, ref l);
            }
        }
    }

    public class Accounts : ObservableCollection<Account>
    {
        public Accounts()
        {
            Add(new Account(AccountType.Active, "Активы"));
            Add(new Account(AccountType.Passive, "Расходы"));
        }
    }  

}
