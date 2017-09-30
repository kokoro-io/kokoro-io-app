using System;
using System.Globalization;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is IConvertible c ? !c.ToBoolean(culture) : value == null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
           => ((IConvertible)(value is IConvertible c ? !c.ToBoolean(culture) : value != null)).ToType(targetType, culture);
    }
}