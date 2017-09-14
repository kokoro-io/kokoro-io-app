using System;
using System.Collections.Specialized;
using System.ComponentModel;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(MessageContent), typeof(MessageContentRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class MessageContentRenderer : ViewRenderer<MessageContent, RichTextBlock>
    {
        public MessageContentRenderer()
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<MessageContent> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    var rtb = new RichTextBlock();
                    rtb.IsTextSelectionEnabled = false;
                    rtb.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
                    ScrollViewer.SetHorizontalScrollBarVisibility(rtb, ScrollBarVisibility.Hidden);
                    ScrollViewer.SetHorizontalScrollMode(rtb, ScrollMode.Disabled);
                    ScrollViewer.SetVerticalScrollBarVisibility(rtb, ScrollBarVisibility.Hidden);
                    ScrollViewer.SetVerticalScrollMode(rtb, ScrollMode.Disabled);

                    SetNativeControl(rtb);
                }
                UpdateBlocks(Control);
            }
        }

        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            Control.MaxWidth = availableSize.Width;
            return base.MeasureOverride(availableSize);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MessageContent.BlocksProperty.PropertyName)
            {
                UpdateBlocks(Control);
            }
            base.OnElementPropertyChanged(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (_Blocks != null && _Blocks.TryGetTarget(out var v))
            {
                v.CollectionChanged -= Blocks_CollectionChanged;
            }
            base.Dispose(disposing);
        }

        private WeakReference<INotifyCollectionChanged> _Blocks;

        private void UpdateBlocks(RichTextBlock rtb)
        {
            if (_Blocks != null && _Blocks.TryGetTarget(out var v))
            {
                v.CollectionChanged -= Blocks_CollectionChanged;
            }

            var bs = Element.Blocks;

            if (bs is INotifyCollectionChanged cc)
            {
                cc.CollectionChanged += Blocks_CollectionChanged;
                _Blocks = new WeakReference<INotifyCollectionChanged>(cc);
            }
            else
            {
                _Blocks = null;
            }

            rtb.Blocks.Clear();

            if (bs != null)
            {
                foreach (var b in bs)
                {
                    if (b is MessageBlock mb)
                    {
                        var para = new Paragraph();

                        foreach (var s in mb.Spans)
                        {
                            para.Inlines.Add(Createinline(s));
                        }

                        rtb.Blocks.Add(para);
                    }
                    else if (b is ListMessageBlock lmb)
                    {
                        foreach (var li in lmb.Items)
                        {
                            var para = new Paragraph();

                            para.TextIndent = 20;

                            para.Inlines.Add(new Run() { Text = "* " });

                            foreach (var s in li.Spans)
                            {
                                para.Inlines.Add(Createinline(s));
                            }

                            rtb.Blocks.Add(para);
                        }
                    }
                    else
                    {
                        var para = new Paragraph();

                        para.Inlines.Add(new Run()
                        {
                            Text = b.ToString()
                        });

                        rtb.Blocks.Add(para);
                    }
                }
            }
        }

        private static Inline Createinline(MessageSpan s)
        {
            if (s.Text == Environment.NewLine)
            {
                return new LineBreak();
            }
            else if (s.Type == MessageSpanType.Hyperlink)
            {
                var hl = new Hyperlink();
                hl.Inlines.Add(new Run() { Text = s.Text });
                return hl;
            }
            else
            {
                return new Run() { Text = s.Text };
            }
        }

        private void Blocks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => UpdateBlocks(Control);
    }
}