using System;
using System.Globalization;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class TargetNullValueConverter : IValueConverter
    {
        public string TargetNullValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? TargetNullValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}