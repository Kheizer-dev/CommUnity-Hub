using System.Globalization;

namespace CommUnity_Hub
{
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasBlotter && hasBlotter)
            {
                return "blotter_icon.png"; // Replace with the actual path to your icon
            }
            return null; // No icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
