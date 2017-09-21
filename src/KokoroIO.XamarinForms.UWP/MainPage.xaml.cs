using XLabs.Ioc;
using XLabs.Platform.Device;

namespace KokoroIO.XamarinForms.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

            var resolver = new SimpleContainer().Register(t => WindowsDevice.CurrentDevice);

            Resolver.ResetResolver(resolver.GetResolver());

            LoadApplication(new XamarinForms.App());
        }
    }
}