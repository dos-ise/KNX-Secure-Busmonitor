using FlyoutPageSample;

namespace KNX_Secure_Busmonitor_MAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        // Instead of setting MainPage, override CreateWindow
        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new AppFlyout());
        }
    }
}