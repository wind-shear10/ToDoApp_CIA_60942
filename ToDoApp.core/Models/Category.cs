using SQLite;
namespace TodoApp.core.Models;
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Unique]
    public string Name { get; set; } = string.Empty;
}
