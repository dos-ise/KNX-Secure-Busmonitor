using KnxMonitor.Abstractions;
using KnxMonitor.Infrastructure;
using KnxMonitor.Services;
using Microsoft.Extensions.Logging;

namespace KnxMonitor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddSingleton<KnxService>();
            builder.Services.AddSingleton<GroupAddressService>();
            builder.Services.AddSingleton<ConnectionService>();
            builder.Services.AddSingleton<IKnxBusDriver, FalconBusDriver>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
