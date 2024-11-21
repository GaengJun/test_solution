using System;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Data;
using System.Windows.Media;

namespace MES.Solution.Helpers.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "대기":
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243));  // Blue
                    case "작업중":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));   // Green
                    case "정상":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));   // Green
                    case "완료":
                        return new SolidColorBrush(Color.FromRgb(156, 39, 176));  // Purple
                    case "경고":
                        return new SolidColorBrush(Color.FromRgb(255, 0, 126));   // red
                                                                                  //설비관리스케줄에서 쓰는거
                    case "점검 연체":
                        return new SolidColorBrush(Color.FromRgb(255,99,71));//빨강
                    case "점검 예정":
                        return new SolidColorBrush(Color.FromRgb(255,165,0));//오렌지
                    case "점검 완료":
                        return new SolidColorBrush(Color.FromRgb(34,139,34));//녹색

                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Grey
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}