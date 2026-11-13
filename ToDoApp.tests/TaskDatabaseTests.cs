using TodoApp.core.Services;

namespace ToDoApp.tests;

// Каждый тест получает отдельный файл SQLite и не затрагивает задачи пользователя.
public class TaskDatabaseTests
{
    private string _path = null!;
    private TaskDatabaseService _db = null!;
    private int _categoryId;
    [SetUp]
    public async Task Setup()
    {
        _path = Path.Combine(Path.GetTempPath(), $"todo-test-{Guid.NewGuid()}.db3");
        _db = new TaskDatabaseService(_path);
        _categoryId = (await _db.GetCategoriesAsync())[0].Id;
    }
    [TearDown]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }
    [Test]
    public async Task TaskSurvivesReopeningDatabase()
    {
        var item = await _db.AddTaskAsync("  Купить хлеб  ", _categoryId);
        await _db.DisposeAsync();
        _db = new TaskDatabaseService(_path);
        var saved = (await _db.GetTasksAsync()).Single();
        Assert.That(saved.Id, Is.EqualTo(item.Id));
        Assert.That(saved.Title, Is.EqualTo("Купить хлеб"));
        Assert.That(saved.CategoryId, Is.EqualTo(_categoryId));
    }
    [Test]
    public async Task CompletionCanBeSetAndCancelled()
    {
        var item = await _db.AddTaskAsync("Задача", _categoryId);
        await _db.SetCompletedAsync(item.Id, true);
        Assert.That((await _db.GetTasksAsync()).Single().IsCompleted, Is.True);
        await _db.SetCompletedAsync(item.Id, false);
        Assert.That((await _db.GetTasksAsync()).Single().IsCompleted, Is.False);
    }
    [Test]
    public async Task DeletingParentDeletesOnlyItsChildren()
    {
        var parent = await _db.AddTaskAsync("Родитель", _categoryId);
        await _db.AddTaskAsync("Подзадача", _categoryId, parent.Id);
        var other = await _db.AddTaskAsync("Другая задача", _categoryId);
        await _db.DeleteTaskAsync(parent.Id);
        Assert.That(await _db.GetTasksAsync(parent.Id), Is.Empty);
        Assert.That((await _db.GetTasksAsync()).Single().Id, Is.EqualTo(other.Id));
    }
    [Test]
    public async Task ChildCanBeCompletedAndDeletedWithoutDeletingParent()
    {
        var parent = await _db.AddTaskAsync("Родитель", _categoryId);
        var child = await _db.AddTaskAsync("Подзадача", _categoryId, parent.Id);
        await _db.SetCompletedAsync(child.Id, true);
        Assert.That((await _db.GetTasksAsync(parent.Id)).Single().IsCompleted, Is.True);
        await _db.DeleteTaskAsync(child.Id);
        Assert.That(await _db.GetTasksAsync(parent.Id), Is.Empty);
        Assert.That(await _db.GetTasksAsync(), Has.Count.EqualTo(1));
    }
    [TestCase("")]
    [TestCase("   ")]
    public void EmptyTitleIsRejected(string title) =>
        Assert.ThrowsAsync<ArgumentException>(() => _db.AddTaskAsync(title, _categoryId));
    [Test]
    public void UnknownCategoryIsRejected() =>
        Assert.ThrowsAsync<ArgumentException>(() => _db.AddTaskAsync("Задача", -1));
    [Test]
    public void UnknownParentIsRejected() =>
        Assert.ThrowsAsync<ArgumentException>(() => _db.AddTaskAsync("Задача", _categoryId, -1));
    [Test]
    public async Task CategoriesAreSavedAndDuplicateNamesRejected()
    {
        var category = await _db.AddCategoryAsync("Работа");
        Assert.That((await _db.GetCategoriesAsync()).Any(c => c.Id == category.Id), Is.True);
        Assert.ThrowsAsync<ArgumentException>(() => _db.AddCategoryAsync(" работа "));
    }
}
