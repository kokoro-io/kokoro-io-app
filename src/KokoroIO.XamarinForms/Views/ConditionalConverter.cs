using System;
using System.Globalization;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class ConditionalConverter<T> : IValueConverter
    {
        public T True { get; set; }
        public T False { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null
                && ((value is bool b && b)))
            {
                return True;
            }
            return False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => object.Equals(True, value);
    }
}