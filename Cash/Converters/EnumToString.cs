using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Cash.Converters
{
    class EnumToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                Type type = value.GetType();
                string name = Enum.GetName(type, value);
                if (name != null)
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    return loader.GetString(type.Name + "/" + name);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
