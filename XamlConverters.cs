using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace ScreenlyManager
{
    public class XamlConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
                return ((bool)value) ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            else if (value is string)
            {
                switch ((string) value)
                {
                    case "video": return "\uE116";
                    case "image": return "\uEB9F";
                    case "webpage": return "\uE128";
                    default: return "\uEB90";
                }
            }
            return new object();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new Exception("Not Implemented");
        }
    }
}
