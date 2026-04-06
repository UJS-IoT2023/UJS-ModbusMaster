using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UJS_ModbusMaster.Helpers
{
    /// <summary>
    /// 连接状态到颜色的转换器
    /// </summary>
    public class ConnectionStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? new SolidColorBrush(Colors.Lime) : new SolidColorBrush(Colors.Gray);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
