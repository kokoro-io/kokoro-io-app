using System;
using System.IO;
using System.Windows.Input;
using Shipwreck.KokoroIO;
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

        #region PostCommand

        public static readonly BindableProperty PostCommandProperty
            = BindableProperty.Create(nameof(PostCommand), typeof(ICommand), typeof(ExpandableEditor));

        public ICommand PostCommand
        {
            get => (ICommand)GetValue(PostCommandProperty);
            set => SetValue(PostCommandProperty, value);
        }

        #endregion PostCommand

        private void ExpandableEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            InvalidateMeasure();
        }

        internal EventHandler<EventArgs<Stream>> _FilePasted;

        public event EventHandler<EventArgs<Stream>> FilePasted
        {
            add { _FilePasted += value; }
            remove { _FilePasted -= value; }
        }
    }
}