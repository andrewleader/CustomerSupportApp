using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CustomerSupportApp.Converters
{
    public class PriorityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string priority)
            {
                return priority switch
                {
                    "High" => new SolidColorBrush(Color.FromRgb(209, 52, 56)),
                    "Normal" => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                    _ => new SolidColorBrush(Color.FromRgb(0, 120, 212))
                };
            }
            return new SolidColorBrush(Color.FromRgb(0, 120, 212));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverse = parameter?.ToString() == "Inverse";
            bool isNull = value == null;

            if (isInverse)
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isNull ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PolitenessLevelColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string politenessLevel)
            {
                return politenessLevel switch
                {
                    "Polite" => new SolidColorBrush(Color.FromRgb(16, 124, 16)), // Green
                    "Somewhat Polite" => new SolidColorBrush(Color.FromRgb(0, 120, 212)), // Blue
                    "Neutral" => new SolidColorBrush(Color.FromRgb(96, 94, 92)), // Gray
                    "Impolite" => new SolidColorBrush(Color.FromRgb(209, 52, 56)), // Red
                    _ => new SolidColorBrush(Color.FromRgb(96, 94, 92))
                };
            }
            return new SolidColorBrush(Color.FromRgb(96, 94, 92));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverse = parameter?.ToString() == "Inverse";
            bool isEmpty = string.IsNullOrWhiteSpace(value?.ToString());

            if (isInverse)
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
