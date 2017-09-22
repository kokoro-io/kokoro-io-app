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
            this.Focused += (_, __) => HasFocus = IsFocused;
            this.Unfocused += (_, __) => HasFocus = IsFocused;
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

        #region SelectionStart

        public static readonly BindableProperty SelectionStartProperty
            = BindableProperty.Create(nameof(SelectionStart), typeof(int), typeof(ExpandableEditor), defaultValue: 0, defaultBindingMode: BindingMode.TwoWay);

        public int SelectionStart
        {
            get => (int)GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        #endregion SelectionStart

        #region SelectionLength

        public static readonly BindableProperty SelectionLengthProperty
            = BindableProperty.Create(nameof(SelectionLength), typeof(int), typeof(ExpandableEditor), defaultValue: 0, defaultBindingMode: BindingMode.TwoWay);

        public int SelectionLength
        {
            get => (int)GetValue(SelectionLengthProperty);
            set => SetValue(SelectionLengthProperty, value);
        }

        #endregion SelectionLength

        #region HasFocus

        public static readonly BindableProperty HasFocusProperty
            = BindableProperty.Create(nameof(HasFocus), typeof(bool), typeof(ExpandableEditor), defaultValue: false, propertyChanged: OnHasFocusChanged);

        public bool HasFocus
        {
            get => (bool)GetValue(HasFocusProperty);
            set => SetValue(HasFocusProperty, value);
        }

        private static void OnHasFocusChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ExpandableEditor ee)
            {
                if (true.Equals(newValue))
                {
                    ee.Focus();
                }
                else
                {
                    ee.Unfocus();
                }
            }
        }

        #endregion HasFocus

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