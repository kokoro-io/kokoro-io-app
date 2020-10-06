using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public abstract class EntitiesLabel : Label
    {
        #region Entities

        public static readonly BindableProperty EntitiesProperty
            = BindableProperty.Create(nameof(Entities), typeof(IEnumerable), typeof(EntitiesLabel), propertyChanged: OnEntitiesChanged);

        public IEnumerable Entities
        {
            get => (IEnumerable)GetValue(EntitiesProperty);
            set => SetValue(EntitiesProperty, value);
        }

        #endregion Entities

        #region SelectCommand

        public static readonly BindableProperty SelectCommandProperty
            = BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(EntitiesLabel));

        public ICommand SelectCommand
        {
            get => (ICommand)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        #endregion SelectCommand

        public abstract string GetText(object item);

        private static void OnEntitiesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var ml = bindable as EntitiesLabel;
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

        public Action TextUpdater;

        private void UpdateText()
        {
            if (TextUpdater != null)
            {
                TextUpdater();
                return;
            }

            var ms = Entities;
            if (ms != null)
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
                        Text = GetText(p)
                    };

                    fs.Spans.Add(span);
                }

                if (fs.Spans.Any())
                {
                    FormattedText = fs;
                    return;
                }
            }
            FormattedText = null;
        }
    }
}