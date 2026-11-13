using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TodoApp.core.Models;

public partial class TaskItem : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CategoryId { get; set; }
    // null — основная задача, число — Id родителя подзадачи.
    public int? ParentId { get; set; }
    [Ignore]
    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;
}
