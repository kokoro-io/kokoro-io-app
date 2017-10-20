using System;
using System.Globalization;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class GridLengthConverter : IValueConverter
    {
        public double Coefficient { get; set; } = 1;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (value as IConvertible)?.ToDouble(culture) ?? 0;

            return new GridLength(v * Coefficient);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => ((GridLength)value).Value / Coefficient;
    }
}