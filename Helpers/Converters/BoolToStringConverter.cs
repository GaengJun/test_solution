using System;
using System.Windows.Data;

namespace MES.Solution.Helpers.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is bool)) return null;
            if (!(parameter is string)) return null;

            bool boolValue = (bool)value;
            string[] options = parameter.ToString().Split('|');

            if (options.Length != 2) return null;

            return boolValue ? options[0] : options[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}