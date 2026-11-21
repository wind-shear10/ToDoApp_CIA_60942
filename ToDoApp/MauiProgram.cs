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

        //  Изучить singleton, Di, что такое builder, services, когда создаются объекты ViewModel и DataBase
        builder.Services.AddSingleton<TaskDatabaseService>(_ => new TaskDatabaseService(
            Path.Combine(FileSystem.AppDataDirectory, "tasks.db3")));
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
