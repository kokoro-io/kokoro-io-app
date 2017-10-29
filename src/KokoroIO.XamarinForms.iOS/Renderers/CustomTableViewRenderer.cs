using System.ComponentModel;
using KokoroIO.XamarinForms.iOS.Renderers;
using KokoroIO.XamarinForms.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CustomTableView), typeof(CustomTableViewRenderer))]

namespace KokoroIO.XamarinForms.iOS.Renderers
{
    public class CustomTableViewRenderer : TableViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<TableView> e)
        {
            base.OnElementChanged(e);
            UpdateDividerHeight();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(CustomTableView.IsSeparatorVisible):
                    UpdateDividerHeight();
                    break;
            }
        }

        private void UpdateDividerHeight()
        {
            Control.SeparatorStyle = (Element as CustomTableView)?.IsSeparatorVisible == false ? UITableViewCellSeparatorStyle.None : UITableViewCellSeparatorStyle.SingleLine;
        }
    }
}