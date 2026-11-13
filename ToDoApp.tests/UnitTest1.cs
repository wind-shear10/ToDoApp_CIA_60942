using TodoApp.core.Services;
using TodoApp.core.ViewModels;

namespace ToDoApp.tests;

// Проверяем команды без окна MAUI: ViewModel использует только обычные классы .NET.
public class ViewModelTests
{
    private string _path = null!;
    private TaskDatabaseService _db = null!;
    private MainViewModel _vm = null!;
    [SetUp]
    public async Task Setup()
    {
        _path = Path.Combine(Path.GetTempPath(), $"todo-vm-{Guid.NewGuid()}.db3");
        _db = new TaskDatabaseService(_path);
        _vm = new MainViewModel(_db);
        await _vm.InitializeAsync();
    }
    [TearDown]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }
    [Test]
    public async Task CommandsCreateSelectCompleteAndDeleteTaskWithChild()
    {
        _vm.NewTaskTitle = "Покупки";
        await _vm.AddTaskCommand.ExecuteAsync(null);
        var parent = _vm.Tasks.Single();
        Assert.That(_vm.NewTaskTitle, Is.Empty);
        await _vm.SelectTaskCommand.ExecuteAsync(parent);
        _vm.NewSubtaskTitle = "Хлеб";
        await _vm.AddSubtaskCommand.ExecuteAsync(null);
        var child = _vm.Subtasks.Single();
        await _vm.SetCompletedAsync(child, true);
        Assert.That(child.IsCompleted, Is.True);
        await _vm.DeleteTaskCommand.ExecuteAsync(parent);
        Assert.That(_vm.Tasks, Is.Empty);
        Assert.That(_vm.Subtasks, Is.Empty);
        Assert.That(_vm.SelectedTask, Is.Null);
        Assert.That(await _db.GetTasksAsync(parent.Id), Is.Empty);
    }
    [Test]
    public async Task ValidationErrorIsVisibleAndDoesNotBlockNextCommand()
    {
        _vm.NewTaskTitle = "   ";
        await _vm.AddTaskCommand.ExecuteAsync(null);
        Assert.That(_vm.ErrorMessage, Is.Not.Empty);
        Assert.That(_vm.IsReady, Is.True);
        Assert.That(_vm.Tasks, Is.Empty);
        _vm.NewTaskTitle = "Задача";
        await _vm.AddTaskCommand.ExecuteAsync(null);
        Assert.That(_vm.ErrorMessage, Is.Empty);
        Assert.That(_vm.Tasks, Has.Count.EqualTo(1));
    }
}
