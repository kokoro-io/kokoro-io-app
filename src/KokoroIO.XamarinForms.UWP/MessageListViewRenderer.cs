using System;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(MessageListView), typeof(MessageListViewRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class MessageListViewRenderer : ListViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ListView> e)
        {
            base.OnElementChanged(e);

            DisposeScrollViewer();

            BindScollViewer(true);
        }

        private void BindScollViewer(bool attachLoaded)
        {
            var sv = List == null ? null
                      : (FrameworkElementAutomationPeer.CreatePeerForElement(List)
                        ?.GetPattern(PatternInterface.Scroll) as ScrollViewerAutomationPeer)?.Owner
                        as ScrollViewer;

            if (sv != null)
            {
                sv.ViewChanged += ScrollViewer_ViewChanged;
                _ScrollViewer = new WeakReference<ScrollViewer>(sv);
            }
            else
            {
                _ScrollViewer = null;

                if (List != null)
                {
                    List.Loaded += List_Loaded;
                }
            }
        }

        private void List_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            BindScollViewer(false);
            ((ListView)sender).Loaded -= List_Loaded;
        }

        protected override void Dispose(bool disposing)
        {
            DisposeScrollViewer();

            base.Dispose(disposing);
        }

        private void DisposeScrollViewer()
        {
            if (_ScrollViewer != null && _ScrollViewer.TryGetTarget(out var s))
            {
                s.ViewChanged -= ScrollViewer_ViewChanged;
            }
        }

        private WeakReference<ScrollViewer> _ScrollViewer;
        private WeakReference<ListViewItem> _LastTop;

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }

            if (!Element.IsRefreshing && List.Items.Count > 0)
            {
                var sv = (ScrollViewer)sender;
                if (_LastTop != null && _LastTop.TryGetTarget(out var lt))
                {
                    lt.StartBringIntoView(new Windows.UI.Xaml.BringIntoViewOptions() { AnimationDesired = false });
                    _LastTop = null;
                    return;
                }

                var item = List.ContainerFromIndex(0) as ListViewItem;

                var gt = item.TransformToVisual(sv);
                var fp = gt.TransformPoint(new Windows.Foundation.Point(0, item.ActualHeight));
                if (fp.Y > 0)
                {
                    _LastTop = new WeakReference<ListViewItem>(item);
                    ((MessageListView)Element)?.RefreshTopCommand?.Execute(null);
                }
            }
        }
    }
}