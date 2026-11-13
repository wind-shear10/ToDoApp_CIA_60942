using Microsoft.Extensions.Logging;

using TodoApp.core.Services;
using TodoApp.core.ViewModels;
namespace ToDoApp;

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

        // DI создаёт цепочку: база -> ViewModel -> страница -> приложение.
        builder.Services.AddSingleton(new TaskDatabaseService(
            Path.Combine(FileSystem.AppDataDirectory, "tasks.db3")));
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
