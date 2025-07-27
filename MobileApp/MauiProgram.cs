using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MobileApp.Services;
using MobileApp.ViewModels;
using MobileApp.Views;
using System.Reflection;
using Newtonsoft.Json;

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
            var configDict = new Dictionary<string, string>
            {

                ["HuggingFace:Model"] = "microsoft/DialoGPT-medium"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            builder.Services.AddSingleton<IConfiguration>(configuration);

            // Register HttpClient and services
            builder.Services.AddHttpClient<IAIChatService, HuggingFaceAIService>();

            // Register pages and view models
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<QuizPage>();
            builder.Services.AddSingleton<FlashcardPage>();
            builder.Services.AddSingleton<AIChatPage>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<QuizViewModel>();
            builder.Services.AddSingleton<FlashcardViewModel>();
            builder.Services.AddSingleton<AIChatViewModel>();

            return builder.Build();
        }
    }
}
