using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cash.Model
{
    public struct Money
    {
        public Currency Curr { get; set; }
        public decimal Amount { get; set; }
    }
}
