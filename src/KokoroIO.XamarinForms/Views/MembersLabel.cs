using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class MembersLabel : Label
    {
        public static readonly BindableProperty MembersProperty
            = BindableProperty.Create(nameof(Members), typeof(IEnumerable<ProfileViewModel>), typeof(MembersLabel), propertyChanged: OnMembersChanged);

        public IEnumerable<ProfileViewModel> Members
        {
            get => (IEnumerable<ProfileViewModel>)GetValue(MembersProperty);
            set => SetValue(MembersProperty, value);
        }

        private static void OnMembersChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var ml = bindable as MembersLabel;
            if (ml != null)
            {
                if (oldValue is INotifyCollectionChanged om)
                {
                    om.CollectionChanged -= ml.Cc_CollectionChanged;
                }

                if (newValue is INotifyCollectionChanged nm)
                {
                    nm.CollectionChanged += ml.Cc_CollectionChanged;
                }
                ml.UpdateText();
            }
        }

        private void Cc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateText();
        }

        internal Action TextUpdater;

        private void UpdateText()
        {
            if (TextUpdater != null)
            {
                TextUpdater();
                return;
            }

            var ms = Members;
            if (ms?.Any() == true)
            {
                var fs = new FormattedString();

                foreach (var p in ms)
                {
                    if (fs.Spans.Any())
                    {
                        fs.Spans.Add(new Span() { Text = " " });
                    }
                    var span = new Span()
                    {
                        Text = '@' + p.ScreenName
                    };

                    fs.Spans.Add(span);
                }

                FormattedText = fs;
            }
            else
            {
                FormattedText = null;
            }
        }

        #region SelectCommand

        public static readonly BindableProperty SelectCommandProperty
            = BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(MembersLabel));

        public ICommand SelectCommand
        {
            get => (ICommand)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        #endregion SelectCommand
    }
}