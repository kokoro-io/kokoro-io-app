using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public class MessageListView : ListView
    {
        public static readonly BindableProperty RefreshTopCommandProperty = BindableProperty.Create(nameof(RefreshTopCommand), typeof(ICommand), typeof(MessageListView));

        public ICommand RefreshTopCommand
        {
            get => (ICommand)GetValue(RefreshTopCommandProperty);
            set => SetValue(RefreshTopCommandProperty, value);
        }
    }
}
