using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LinuxFileExplorer
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter != null && parameter.ToString() == "Inverse";
                return (invert ? !boolValue : boolValue) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }

    public class SizeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size && size > 0)
            {
                double kbSize = size / 1024.0;
                return $"{kbSize:N2} KB";
            }
            else if (value is int intSize && intSize > 0)
            {
                double kbSize = intSize / 1024.0;
                return $"{kbSize:N2} KB";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
