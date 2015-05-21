using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cash.Model
{
    public class Currency
    {
        public static Currency Default = new Currency();
        private string iso;
        public string ISO { get { return iso; } }
        public string Name { get; set; }
        public Currency() :
            this(RegionInfo.CurrentRegion.ISOCurrencySymbol)
        { 
        }
        public Currency(string iso)
        {
            this.iso = iso.ToUpper();
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            Name = loader.GetString("Currency/" + iso);
        }
    }

    public class Currencies : List<Currency>
    {
        public Currencies()
        {
            Add(new Currency("RUB"));
            Add(new Currency("USD"));
            Add(new Currency("EUR"));
        }
    }  
}
