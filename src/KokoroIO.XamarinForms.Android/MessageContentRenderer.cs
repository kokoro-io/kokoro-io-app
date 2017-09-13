using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Android.Text;
using Android.Widget;
using KokoroIO.XamarinForms.Droid;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(MessageContent), typeof(MessageContentRenderer))]

namespace KokoroIO.XamarinForms.Droid
{
    public sealed class MessageContentRenderer : ViewRenderer<MessageContent, TextView>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<MessageContent> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    SetNativeControl(new TextView(Context));
                }
                UpdateBlocks(Control);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MessageContent.BlocksProperty.PropertyName)
            {
                UpdateBlocks(Control);
            }
            base.OnElementPropertyChanged(sender, e);
        }

        private WeakReference<INotifyCollectionChanged> _Blocks;

        private void UpdateBlocks(TextView textView)
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

            if (bs == null)
            {
                textView.Text = "";
                return;
            }

            var builder = new SpannableStringBuilder();

            foreach (var b in bs)
            {
                if (builder.Length() > 0)
                {
                    builder.Append(System.Environment.NewLine);
                    builder.Append(System.Environment.NewLine);
                }

                if (b is MessageBlock mb)
                {
                    foreach (var s in mb.Spans)
                    {
                        AppendSpan(s, builder);
                    }
                }
                else if (b is ListMessageBlock lmb)
                {
                    foreach (var li in lmb.Items)
                    {
                        if (builder.Length() > 0)
                        {
                            builder.Append(System.Environment.NewLine);
                        }

                        builder.Append("    * ");
                        foreach (var s in li.Spans)
                        {
                            AppendSpan(s, builder);
                        }
                    }
                }
                else
                {
                    builder.Append(b.ToString());
                }
            }

            textView.TextFormatted = builder;
        }

        private static void AppendSpan(MessageSpan s, SpannableStringBuilder builder)
        {
            builder.Append(s.Text);
        }

        private void Blocks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => UpdateBlocks(Control);
    }
}