using System;
using Windows.UI.Xaml.Data;

namespace ScreenlyManager.Converters
{
    public class MimeTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
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
