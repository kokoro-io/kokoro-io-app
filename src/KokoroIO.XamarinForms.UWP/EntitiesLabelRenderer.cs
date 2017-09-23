using System;
using System.Linq;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Windows.UI.Xaml.Documents;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(EntitiesLabel), typeof(EntitiesLabelRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class EntitiesLabelRenderer : LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if (Element is EntitiesLabel ml)
            {
                ml.TextUpdater = UpdateEntitiesText;
            }
        }

        private void UpdateEntitiesText()
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
            if (Element is EntitiesLabel ml)
            {
                if (ml.Entities != null)
                {
                    foreach (var m in ml.Entities)
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
                            Text = ml.GetText(m)
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
                var sc = ((Run)sender.Inlines[0]).Text;

                var ml = Element as EntitiesLabel;
                var mb = ml?.Entities.Cast<object>()?.FirstOrDefault(m => ml.GetText(m).Equals(sc, StringComparison.OrdinalIgnoreCase));

                c.Execute(mb);
            }
        }
    }
}