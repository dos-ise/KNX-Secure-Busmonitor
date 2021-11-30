using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace KNX_Secure_Busmonitor.MAUI
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}