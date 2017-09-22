using System;
using System.Linq;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using KokoroIO.XamarinForms.Droid;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(MembersLabel), typeof(MembersLabelRenderer))]

namespace KokoroIO.XamarinForms.Droid
{
    public sealed class MembersLabelRenderer : LabelRenderer
    {
        private class ScreenNameSpan : ClickableSpan
        {
            private readonly MembersLabelRenderer _Renderer;
            private readonly ProfileViewModel _Profile;
            public ScreenNameSpan(MembersLabelRenderer r, ProfileViewModel p)
            {
                _Renderer = r;
                _Profile = p;
            }

            public override void OnClick(Android.Views.View widget)
                => _Renderer.OnMemberClick(_Profile);
        }

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

            if (Element is MembersLabel ml)
            {
                if (ml.Members?.Any() != true)
                {
                    Control.Text = null;
                    return;
                }

                var ssb = new SpannableStringBuilder();
                foreach (var m in ml.Members)
                {
                    if (ssb.Length() > 0)
                    {
                        ssb.Append(' ');
                    }

                    var i = ssb.Length();

                    ssb.Append('@');
                    ssb.Append(m.ScreenName);

                    ssb.SetSpan(new ScreenNameSpan(this, m), i, ssb.Length(), SpanTypes.ExclusiveExclusive);
                }

                Control.TextFormatted = ssb;
                Control.MovementMethod = LinkMovementMethod.Instance;
            }
        }

        private void OnMemberClick(ProfileViewModel mb)
        {
            var c = (Element as MembersLabel)?.SelectCommand;

            if (c != null)
            {
                c.Execute(mb);
            }
        }
    }
}