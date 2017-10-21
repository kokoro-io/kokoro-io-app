// From https://github.com/xamarin/Xamarin.Forms/blob/f6b42cc2717944f5eda019c64f5fa0065ec7b0d6/Xamarin.Forms.Platform.WinRT/TableViewRenderer.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KokoroIO.XamarinForms.UWP;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(TableView), typeof(MatomonaTableViewRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class MatomonaTableViewRenderer : ViewRenderer<TableView, Windows.UI.Xaml.Controls.ListView>
    {
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

            var list = Element.Root?.SelectMany(l => l).ToList() ?? new List<Cell>(0);

            if (Control.ItemsSource is ObservableCollection<Cell> dest)
            {
                var j = 0;

                for (int i = 0; i < dest.Count; i++)
                {
                    var cell = dest[i];
                    var cellNodeIndex = list.IndexOf(cell, j);

                    if (cellNodeIndex < j)
                    {
                        dest.RemoveAt(i--);
                    }
                    else
                    {
                        while (j <= cellNodeIndex)
                        {
                            var n = list[j++];

                            if (n != cell)
                            {
                                dest.Insert(i++, n);
                            }
                        }
                    }
                }

                for (; j < list.Count; j++)
                {
                    dest.Add(list[j]);
                }
            }
            else
            {
                Control.ItemsSource = new ObservableCollection<Cell>(list);
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