using Microsoft.Extensions.Logging;
using MobileApp.Views;
using MobileApp.ViewModels;

namespace MobileApp
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<QuizPage>();
            builder.Services.AddSingleton<FlashcardPage>();

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<QuizViewModel>();
            builder.Services.AddSingleton<FlashcardViewModel>();

            return builder.Build();
        }
    }
}
