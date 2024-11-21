using System;
using System.Windows.Data;

namespace MES.Solution.Helpers.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return null;

            string[] options = parameter.ToString().Split('|');
            if (options.Length != 2) return value.ToString();

            if (value is MES.Solution.Models.Equipment.OperationMode mode)
            {
                return mode == Models.Equipment.OperationMode.Automatic ? options[0] : options[1];
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}