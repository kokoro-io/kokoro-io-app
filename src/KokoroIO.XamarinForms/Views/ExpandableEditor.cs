using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class ExpandableEditor : Editor
    {
        public ExpandableEditor()
        {
            this.TextChanged += ExpandableEditor_TextChanged;
        }

        private void ExpandableEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            InvalidateMeasure();
        }
    }
}