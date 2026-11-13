using TodoApp.core.Models;
using TodoApp.core.ViewModels;
namespace ToDoApp;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel; // Источник свойств и команд для Binding.
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
    // У CheckBox нет Command: View только пересылает событие во ViewMode
    // Бизнес-логика и запросы к БД остаются за пределами страницы.
    private async void OnCompletedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox { BindingContext: TaskItem item } box) return;
        if (item.IsCompleted == e.Value) return;
        await _viewModel.SetCompletedAsync(item, e.Value);
        box.IsChecked = item.IsCompleted; // При ошибке записи восстанавливаем значение.
    }
}
