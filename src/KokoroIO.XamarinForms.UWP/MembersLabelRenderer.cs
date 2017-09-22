using System;
using System.Linq;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Windows.UI.Xaml.Documents;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(MembersLabel), typeof(MembersLabelRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class MembersLabelRenderer : LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if (Element is MembersLabel ml)
            {
                ml.TextUpdater = UpdateMemberText;
            }
        }

        private void UpdateMemberText()
        {
            if (Control == null)
            {
                return;
            }

            foreach (var hl in Control.Inlines.OfType<Hyperlink>())
            {
                hl.Click -= Hl_Click;
            }
            Control.Inlines.Clear();
            if (Element is MembersLabel ml)
            {
                if (ml.Members != null)
                {
                    foreach (var m in ml.Members)
                    {
                        if (Control.Inlines.Any())
                        {
                            Control.Inlines.Add(new Run() { Text = " " });
                        }

                        var hl = new Hyperlink();

                        hl.TextDecorations = Control.TextDecorations;
                        hl.Foreground = Control.Foreground;
                        hl.FontFamily = Control.FontFamily;
                        hl.FontSize = Control.FontSize;
                        hl.FontStretch = Control.FontStretch;
                        hl.FontStyle = Control.FontStyle;
                        hl.FontWeight = Control.FontWeight;
                        hl.Inlines.Add(new Run()
                        {
                            Text = "@" + m.ScreenName
                        });
                        hl.Click += Hl_Click;

                        Control.Inlines.Add(hl);
                    }
                }
            }
        }

        private void Hl_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            var c = (Element as MembersLabel)?.SelectCommand;

            if (c != null)
            {
                var sc = ((Run)sender.Inlines[0]).Text.Substring(1);

                var mb = (Element as MembersLabel)?.Members?.FirstOrDefault(m => m.ScreenName.Equals(sc, StringComparison.OrdinalIgnoreCase));

                c.Execute(mb);
            }
        }
    }
}