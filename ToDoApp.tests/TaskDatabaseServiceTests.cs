using FluentAssertions;
using SQLite;
using TodoApp.core.Models;
using TodoApp.core.Services;

namespace ToDoApp.tests;

[NonParallelizable, NUnit.Framework.Category("Integration")]
public class TaskDatabaseServiceTests
{
    private SQLiteAsyncConnection _connection = null!;
    private TaskDatabaseService _db = null!;
    private List<Category> _defaults = null!;
    private int CategoryId => _defaults[0].Id;
    
    [OneTimeSetUp]
    public async Task OpenDatabase()
    {
        _connection = new SQLiteAsyncConnection(":memory:");
        _db = new TaskDatabaseService(_connection);
        _defaults = await _db.GetCategoriesAsync();
    }

    [SetUp]
    public async Task ResetRows()
    {
        await _connection.DeleteAllAsync<TaskItem>();
        foreach (var category in await _connection.Table<Category>().ToListAsync())
            if (!_defaults.Any(c => c.Id == category.Id))
                await _connection.DeleteAsync(category);
    }

    [OneTimeTearDown]
    public async Task CloseDatabase() => await _db.DisposeAsync();

    [Test]
    public async Task GetCategoriesReturnsDefaultNames()
    {
        var categories = await _db.GetCategoriesAsync();

        categories.Select(c => c.Name).Should().BeEquivalentTo(new[] { "Личное", "Учёба", "Покупки" });
    }

    [Test]
    public async Task AddedCategorySavesName()
    {
        var category = await _db.AddCategoryAsync("Работа");

        (await _connection.GetAsync<Category>(category.Id)).Name.Should().Be("Работа");
    }

    [Test]
    public async Task AddCategoryRejectsDuplicateName()
    {
        await _db.AddCategoryAsync("Работа");

        Func<Task> act = () => _db.AddCategoryAsync(" работа ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task AddTaskSavesTrimmedTitle()
    {
        var task = await _db.AddTaskAsync("  Купить хлеб  ", CategoryId);

        (await _connection.GetAsync<TaskItem>(task.Id)).Title.Should().Be("Купить хлеб");
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task AddTaskRejectsEmptyTitle(string title)
    {
        Func<Task> act = () => _db.AddTaskAsync(title, CategoryId);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task AddTaskRejectsUnknownCategory()
    {
        Func<Task> act = () => _db.AddTaskAsync("Хлеб", -1);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task AddSubtaskRejectsUnknownParent()
    {
        Func<Task> act = () => _db.AddTaskAsync("Хлеб", CategoryId, -1);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SetCompletedSavesRequestedValue(bool completed)
    {
        var item = new TaskItem { Title = "Хлеб", CategoryId = CategoryId, IsCompleted = !completed };
        await _connection.InsertAsync(item);

        await _db.SetCompletedAsync(item.Id, completed);

        (await _connection.GetAsync<TaskItem>(item.Id)).IsCompleted.Should().Be(completed);
    }

    [Test]
    public async Task DeleteTaskRemovesTask()
    {
        var task = await _db.AddTaskAsync("Покупки", CategoryId);

        await _db.DeleteTaskAsync(task.Id);

        (await _connection.FindAsync<TaskItem>(task.Id)).Should().BeNull();
    }

    [Test]
    public async Task DeleteParentRemovesChild()
    {
        var parent = await _db.AddTaskAsync("Покупки", CategoryId);
        var child = await _db.AddTaskAsync("Хлеб", CategoryId, parent.Id);

        await _db.DeleteTaskAsync(parent.Id);

        (await _connection.FindAsync<TaskItem>(child.Id)).Should().BeNull();
    }

    [Test]
    public async Task DeleteTaskKeepsOtherTask()
    {
        var task = await _db.AddTaskAsync("Покупки", CategoryId);
        var other = await _db.AddTaskAsync("Учёба", CategoryId);

        await _db.DeleteTaskAsync(task.Id);

        (await _connection.FindAsync<TaskItem>(other.Id)).Should().NotBeNull();
    }

    [Test]
    public async Task GetTasksReturnsChildrenOfSelectedParent()
    {
        var parent = await _db.AddTaskAsync("Покупки", CategoryId);
        var child = await _db.AddTaskAsync("Хлеб", CategoryId, parent.Id);
        var other = await _db.AddTaskAsync("Учёба", CategoryId);
        await _db.AddTaskAsync("Учебник", CategoryId, other.Id);

        var tasks = await _db.GetTasksAsync(parent.Id);

        tasks.Select(t => t.Id).Should().Equal(child.Id);
    }

    [Test]
    public async Task AddCategoryRejectsBlankName()
    {
        Func<Task> act = () => _db.AddCategoryAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task AddTaskRejectsTitleLongerThan200Characters()
    {
        var title = new string('x', 201);

        Func<Task> act = () => _db.AddTaskAsync(title, CategoryId);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task AddSubtaskUsesParentCategory()
    {
        var parent = await _db.AddTaskAsync("Покупки", CategoryId);
        var otherCategory = await _db.AddCategoryAsync("Работа");

        var child = await _db.AddTaskAsync("Хлеб", otherCategory.Id, parent.Id);

        (await _connection.GetAsync<TaskItem>(child.Id)).CategoryId.Should().Be(parent.CategoryId);
    }

    [Test]
    public async Task GetRootTasksOmitsSubtasks()
    {
        var parent = await _db.AddTaskAsync("Покупки", CategoryId);
        await _db.AddTaskAsync("Хлеб", CategoryId, parent.Id);

        var roots = await _db.GetTasksAsync();

        roots.Select(t => t.Id).Should().Equal(parent.Id);
    }

    [Test]
    public async Task AddSubtaskRejectsNestedSubtask()
    {
        var parent = await _db.AddTaskAsync("Учёба", CategoryId);
        var child = await _db.AddTaskAsync("Лабораторная", CategoryId, parent.Id);

        Func<Task> act = () => _db.AddTaskAsync("Отчёт", CategoryId, child.Id);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task SetCompletedRejectsUnknownTask()
    {
        Func<Task> act = () => _db.SetCompletedAsync(-1, true);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
