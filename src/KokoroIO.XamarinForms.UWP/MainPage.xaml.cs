using XLabs.Ioc;
using XLabs.Platform.Device;

namespace KokoroIO.XamarinForms.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            var resolver = new SimpleContainer().Register(t => WindowsDevice.CurrentDevice);

            Resolver.SetResolver(resolver.GetResolver());

            LoadApplication(new KokoroIO.XamarinForms.App());
        }
    }
}