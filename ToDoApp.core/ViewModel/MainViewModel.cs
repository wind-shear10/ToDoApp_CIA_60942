using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.core.Models;
using TodoApp.core.Services;
namespace TodoApp.core.ViewModels;
public partial class MainViewModel : ObservableObject
{
    private readonly TaskDatabaseService _database;
    private bool _loaded;
    public MainViewModel(TaskDatabaseService database) => _database = database;
    // Коллекции уведомляют экран об добавлении и удалении элементов
    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> Subtasks { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    [ObservableProperty] private string _newTaskTitle = string.Empty;
    [ObservableProperty] private string _newSubtaskTitle = string.Empty;
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private TaskItem? _selectedTask;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady))]
    private bool _isBusy;
    public bool IsReady => !IsBusy;

    // Общая обработка ошибок и блокировка повторных операций во время записи.
    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try { await action(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
    public Task InitializeAsync() => RunAsync(async () =>
    {
        if (_loaded) return;
        var categories = await _database.GetCategoriesAsync();
        var tasks = await _database.GetTasksAsync();
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);
        SelectedCategory = Categories.FirstOrDefault();
        Tasks.Clear();
        foreach (var item in tasks) Tasks.Add(item);
        _loaded = true;
    });
    [RelayCommand]
    private Task AddTaskAsync() => RunAsync(async () =>
    {
        if (SelectedCategory is null) throw new ArgumentException("Выбери категорию.");
        var item = await _database.AddTaskAsync(NewTaskTitle, SelectedCategory.Id);
        Tasks.Add(item); 
        NewTaskTitle = string.Empty;
    });
    [RelayCommand]
    private Task AddCategoryAsync() => RunAsync(async () =>
    {
        var category = await _database.AddCategoryAsync(NewCategoryName);
        Categories.Add(category);
        SelectedCategory = category;
        NewCategoryName = string.Empty;
    });
    [RelayCommand]
    private Task SelectTaskAsync(TaskItem? item) => RunAsync(async () =>
    {
        if (item is null) return;
        var children = await _database.GetTasksAsync(item.Id);
        SelectedTask = item;
        NewSubtaskTitle = string.Empty;
        Subtasks.Clear();
        foreach (var child in children) Subtasks.Add(child);
    });
    [RelayCommand]
    private Task AddSubtaskAsync() => RunAsync(async () =>
    {
        if (SelectedTask is null) throw new ArgumentException("Сначала выберите задачу слева.");
        Subtasks.Add(await _database.AddTaskAsync(NewSubtaskTitle, SelectedTask.CategoryId, SelectedTask.Id));
        NewSubtaskTitle = string.Empty;
    });
    [RelayCommand]
    private Task DeleteTaskAsync(TaskItem? item) => RunAsync(async () =>
    {
        if (item is null) return;
        await _database.DeleteTaskAsync(item.Id);
        if (item.ParentId.HasValue) Subtasks.Remove(item);
        else Tasks.Remove(item);
        if (SelectedTask?.Id == item.Id) { SelectedTask = null; Subtasks.Clear(); }
    });
    public Task SetCompletedAsync(TaskItem item, bool value) => RunAsync(async () =>
    {
        await _database.SetCompletedAsync(item.Id, value);
        item.IsCompleted = value;
    });
}
