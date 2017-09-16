using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class ExpandableEditor : Editor
    {
        public ExpandableEditor()
        {
            this.TextChanged += ExpandableEditor_TextChanged;
        }

        #region Placeholder

        public static readonly BindableProperty PlaceholderProperty
            = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(MessageWebView));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        #endregion Placeholder

        private void ExpandableEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            InvalidateMeasure();
        }
    }
}