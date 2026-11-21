using SQLite;
using TodoApp.core.Models;
namespace TodoApp.core.Services;
public sealed class TaskDatabaseService : IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _db;
    private readonly Lazy<Task> _initialization;
    public TaskDatabaseService(string path) : this(new SQLiteAsyncConnection(path)) { }
    
    public TaskDatabaseService(SQLiteAsyncConnection connection)
    {
        _db = connection;
        _initialization = new Lazy<Task>(InitializeAsync);
    }
    private async Task InitializeAsync()
    {
        var categoryTableResult = await _db.CreateTableAsync<Category>();
        await _db.CreateTableAsync<TaskItem>();
        
        if (categoryTableResult == CreateTableResult.Created)
        {
            await _db.InsertAllAsync(new[] { new Category { Name = "Личное" }, new Category { Name = "Учёба" }, new Category { Name = "Покупки" } });
        }
    }
    public async Task<List<Category>> GetCategoriesAsync()
    {
        await _initialization.Value;
        return await _db.Table<Category>().OrderBy(c => c.Id).ToListAsync();
    }
    public async Task<Category> AddCategoryAsync(string name)
    {
        await _initialization.Value;
        name = ValidateTitle(name);
        if ((await GetCategoriesAsync()).Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Такая категория уже существует.");
        var category = new Category { Name = name };
        await _db.InsertAsync(category);
        return category;
    }
    public async Task<List<TaskItem>> GetTasksAsync(int? parentId = null)
    {
        await _initialization.Value;
        var items = parentId.HasValue
            ? await _db.Table<TaskItem>().Where(t => t.ParentId == parentId).OrderBy(t => t.Id).ToListAsync()
            : await _db.Table<TaskItem>().Where(t => t.ParentId == null).OrderBy(t => t.Id).ToListAsync();
        var categories = await GetCategoriesAsync();
        foreach (var item in items)
            item.CategoryName = categories.FirstOrDefault(c => c.Id == item.CategoryId)?.Name ?? "Без категории";
        return items;
    }
    public async Task<TaskItem> AddTaskAsync(string title, int categoryId, int? parentId = null)
    {
        await _initialization.Value;
        title = ValidateTitle(title);
        if (await _db.FindAsync<Category>(categoryId) is null)
            throw new ArgumentException("Выбери существующую категорию.");
        if (parentId.HasValue)
        {
            var parent = await _db.FindAsync<TaskItem>(parentId.Value);
            if (parent is null || parent.ParentId.HasValue)
                throw new ArgumentException("Подзадачу можно добавить только к основной задаче.");
            categoryId = parent.CategoryId;
        }
        var item = new TaskItem { Title = title, CategoryId = categoryId, ParentId = parentId };
        await _db.InsertAsync(item); // SQLite сама назначает Id.
        item.CategoryName = (await _db.FindAsync<Category>(categoryId)).Name;
        return item;
    }
    public async Task SetCompletedAsync(int id, bool completed)
    {
        await _initialization.Value;
        var item = await _db.FindAsync<TaskItem>(id) ?? throw new ArgumentException("Задача уже удалена.");
        item.IsCompleted = completed;
        await _db.UpdateAsync(item);
    }
    public async Task DeleteTaskAsync(int id)
    {
        await _initialization.Value;
        // Транзакция гарантирует совместное удаление родителя и его подзадач.
        await _db.RunInTransactionAsync(connection =>
        {
            foreach (var child in connection.Table<TaskItem>().Where(t => t.ParentId == id).ToList())
                connection.Delete(child);
            connection.Delete<TaskItem>(id);
        });
    }
    private static string ValidateTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Название не должно быть пустым.");
        if (text.Trim().Length > 200) throw new ArgumentException("Не больше 200 символов в названии.");
        return text.Trim();
    }
    public async ValueTask DisposeAsync() => await _db.CloseAsync();
}
