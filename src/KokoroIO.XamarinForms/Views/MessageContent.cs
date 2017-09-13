using System.Collections.Generic;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class MessageContent : View
    {
        public static readonly BindableProperty BlocksProperty = BindableProperty.Create(nameof(Blocks), typeof(IEnumerable<MessageBlockBase>), typeof(MessageContent));

        public IEnumerable<MessageBlockBase> Blocks
        {
            get => GetValue(BlocksProperty) as IEnumerable<MessageBlockBase>;
            set => SetValue(BlocksProperty, value);
        }
    }
}