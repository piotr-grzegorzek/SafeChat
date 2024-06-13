using Microsoft.Extensions.Logging;

namespace SafeChat
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts((fonts) => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("********** OMG! FirstChanceException **********");
                System.Diagnostics.Debug.WriteLine(e.Exception);
            };

            return builder.Build();
        }
    }
}
