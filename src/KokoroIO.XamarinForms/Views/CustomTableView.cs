using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class CustomTableView : TableView
    {
        #region IsSeparatorVisible

        public static readonly BindableProperty IsSeparatorVisibleProperty
            = BindableProperty.Create(nameof(IsSeparatorVisible), typeof(bool), typeof(CustomTableView), false);

        public bool IsSeparatorVisible
        {
            get => (bool)GetValue(IsSeparatorVisibleProperty);
            set => SetValue(IsSeparatorVisibleProperty, value);
        }

        #endregion IsSeparatorVisible
    }
}