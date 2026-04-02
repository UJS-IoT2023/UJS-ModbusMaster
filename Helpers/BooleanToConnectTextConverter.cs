using System;
using System.Globalization;
using System.Windows.Data;

namespace UJS_ModbusMaster.Helpers
{
    /// <summary>
    /// 布尔值到连接按钮文本的转换器
    /// </summary>
    public class BooleanToConnectTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? "断开连接" : "连接串口";
            }
            return "连接串口";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
