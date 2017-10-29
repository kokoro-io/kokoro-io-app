// From https://github.com/xamarin/Xamarin.Forms/blob/f6b42cc2717944f5eda019c64f5fa0065ec7b0d6/Xamarin.Forms.Platform.WinRT/TableViewRenderer.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using KokoroIO.XamarinForms.UWP.Renderers;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(TableView), typeof(MatomonaTableViewRenderer))]

namespace KokoroIO.XamarinForms.UWP.Renderers
{
    public sealed class MatomonaTableViewRenderer : ViewRenderer<TableView, Windows.UI.Xaml.Controls.ListView>
    {
        private sealed class VectorChangedEventArgs : IVectorChangedEventArgs
        {
            public CollectionChange CollectionChange { get; set; }
            public uint Index { get; set; }
        }

        private sealed class TableSectionWrapper : ICollectionViewGroup, IObservableVector<object>, INotifyPropertyChanged
        {
            public TableSectionWrapper(TableSection section)
            {
                Section = section;
                Section.PropertyChanged += (s, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(Title):
                        case nameof(Count):
                        case "Item[]":
                            PropertyChanged?.Invoke(this, e);
                            break;
                    }
                };
                Section.CollectionChanged += Section_CollectionChanged;
            }

            private void Section_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                var vc = VectorChanged;
                if (vc == null)
                {
                    return;
                }

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewStartingIndex >= 0)
                        {
                            for (var i = 0; i < e.NewItems.Count; i++)
                            {
                                vc(this, new VectorChangedEventArgs()
                                {
                                    CollectionChange = CollectionChange.ItemInserted,
                                    Index = (uint)(e.NewStartingIndex + i)
                                });
                            }
                            return;
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldStartingIndex >= 0)
                        {
                            for (var i = e.OldItems.Count - 1; i >= 0; i--)
                            {
                                vc(this, new VectorChangedEventArgs()
                                {
                                    CollectionChange = CollectionChange.ItemRemoved,
                                    Index = (uint)(e.OldStartingIndex + i)
                                });
                            }
                            return;
                        }
                        break;
                }

                vc(this, new VectorChangedEventArgs()
                {
                    CollectionChange = CollectionChange.Reset
                });
            }

            public string Title => Section.Title;

            internal TableSection Section { get; }

            public object Group => Section;

            public IObservableVector<object> GroupItems
                => new CollectionViewSource() { Source = Section }.View;

            public event VectorChangedEventHandler<object> VectorChanged;

            public event PropertyChangedEventHandler PropertyChanged;

            public int IndexOf(object item)
                => Section.IndexOf((Cell)item);

            public void Insert(int index, object item)
                => Section.Insert(index, (Cell)item);

            public void RemoveAt(int index)
                => Section.RemoveAt(index);

            public object this[int index]
            {
                get => Section[index];
                set => Section[index] = (Cell)value;
            }

            public void Add(object item)
                => Section.Add((Cell)item);

            public void Clear()
                => Section.Clear();

            public bool Contains(object item)
                => Section.Contains(item as Cell);

            public void CopyTo(object[] array, int arrayIndex)
                => Section.CopyTo((Cell[])array, arrayIndex);

            public bool Remove(object item)
                => Section.Remove(item as Cell);

            public int Count => Section.Count;

            public bool IsReadOnly => false;

            public IEnumerator<object> GetEnumerator()
                => Section.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => Section.GetEnumerator();
        }

        private bool _ignoreSelectionEvent;

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            SizeRequest result = base.GetDesiredSize(widthConstraint, heightConstraint);
            result.Minimum = new Size(40, 40);
            return result;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<TableView> e)
        {
            if (e.OldElement != null)
            {
                e.OldElement.ModelChanged -= OnModelChanged;
            }

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    SetNativeControl(new Windows.UI.Xaml.Controls.ListView
                    {
                        ItemContainerStyle = (Windows.UI.Xaml.Style)Windows.UI.Xaml.Application.Current.Resources["FormsListViewItem"],
                        ItemTemplate = (Windows.UI.Xaml.DataTemplate)Windows.UI.Xaml.Application.Current.Resources["CellTemplate"],
                        GroupStyle = { new GroupStyle { HidesIfEmpty = false, HeaderTemplate = (Windows.UI.Xaml.DataTemplate)Windows.UI.Xaml.Application.Current.Resources["TableSection"] } },
                        HeaderTemplate = (Windows.UI.Xaml.DataTemplate)Windows.UI.Xaml.Application.Current.Resources["TableRoot"],
                        SelectionMode = ListViewSelectionMode.Single
                    });

                    Control.SetBinding(ItemsControl.ItemsSourceProperty, new Windows.UI.Xaml.Data.Binding());
                    Control.SelectionChanged += OnSelectionChanged;
                }

                e.NewElement.ModelChanged += OnModelChanged;
                OnModelChanged(e.NewElement, EventArgs.Empty);
            }

            base.OnElementChanged(e);
        }

        private void OnModelChanged(object sender, EventArgs e)
        {
            Control.Header = Element.Root;

            // This auto-selects the first item in the new DataContext, so we just null it and ignore the selection
            // as this selection isn't driven by user input
            _ignoreSelectionEvent = true;

            if (Control.DataContext is CollectionViewSource cvs
                && cvs.Source is ObservableCollection<TableSectionWrapper> dest)
            {
                var j = 0;

                for (int i = 0; i < dest.Count; i++)
                {
                    var c = dest[i];
                    var cell = c.Section;
                    var cellNodeIndex = Element.Root.IndexOf(cell);

                    if (cellNodeIndex < j)
                    {
                        dest.RemoveAt(i--);
                    }
                    else
                    {
                        while (j <= cellNodeIndex)
                        {
                            var n = Element.Root[j++];

                            if (n != cell)
                            {
                                dest.Insert(i++, new TableSectionWrapper(n));
                            }
                        }
                    }
                }

                for (; j < Element.Root.Count; j++)
                {
                    dest.Add(new TableSectionWrapper(Element.Root[j]));
                }
            }
            else
            {
                Control.DataContext = new CollectionViewSource()
                {
                    Source = new ObservableCollection<TableSectionWrapper>(Element.Root.Select(s => new TableSectionWrapper(s)).ToList()),
                    IsSourceGrouped = true
                };
            }

            _ignoreSelectionEvent = false;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionEvent)
            {
                foreach (object item in e.AddedItems)
                {
                    var cell = item as Cell;
                    if (cell != null)
                    {
                        if (cell.IsEnabled)
                            Element.Model.RowSelected(cell);
                        break;
                    }
                }
            }

            Control.SelectedItem = null;
        }
    }
}