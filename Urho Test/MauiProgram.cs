using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace Urho_Test {
    public static class MauiProgram {
        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
		builder.Logging.AddDebug();
#endif
            builder.ConfigureMauiHandlers(handlers => {
                handlers.AddHandler(typeof(UrhoSurface), typeof(UrhoSurfaceHandler));
            });
            return builder.Build();
        }
    }
}