using Microsoft.Data.Sqlite;
using TodoApi.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests;

public class TodoServiceTests : IDisposable
{
    private readonly SqliteConnection _keepAliveConnection;
    private readonly TodoService _service;

    public TodoServiceTests()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        _keepAliveConnection = new SqliteConnection(connectionString);
        _keepAliveConnection.Open();
        InitializeSchema();

        _service = new TodoService(connectionString);
    }

    private void InitializeSchema()
    {
        var command = _keepAliveConnection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Todos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            )
        ";
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _keepAliveConnection.Close();
        _keepAliveConnection.Dispose();
    }

    [Fact]
    public void CreateTodo_ValidRequest_ReturnsTodoWithGeneratedId()
    {
        var request = new CreateTodoRequest { Title = "My Task", Description = "Details" };

        var result = _service.CreateTodo(request);

        Assert.True(result.Id > 0);
        Assert.Equal("My Task", result.Title);
        Assert.Equal("Details", result.Description);
        Assert.False(result.IsCompleted);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }


    [Fact]
    public void GetAllTodos_EmptyDatabase_ReturnsEmptyList()
    {
        var result = _service.GetAllTodos();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAllTodos_MultipleTodos_ReturnsAll()
    {
        _service.CreateTodo(new CreateTodoRequest { Title = "Task A" });
        _service.CreateTodo(new CreateTodoRequest { Title = "Task B" });

        var result = _service.GetAllTodos();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void GetTodoById_ExistingId_ReturnsTodo()
    {
        var created = _service.CreateTodo(new CreateTodoRequest { Title = "Find Me" });

        var result = _service.GetTodoById(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Find Me", result.Title);
    }

    [Fact]
    public void GetTodoById_NonExistingId_ReturnsNull()
    {
        var result = _service.GetTodoById(99999);

        Assert.Null(result);
    }

    [Fact]
    public void UpdateTodo_ExistingId_ReturnsUpdatedTodo()
    {
        var created = _service.CreateTodo(new CreateTodoRequest { Title = "Original" });
        var request = new UpdateTodoRequest { Title = "Updated", Description = "New desc", IsCompleted = true };

        var result = _service.UpdateTodo(created.Id, request);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Updated", result.Title);
        Assert.Equal("New desc", result.Description);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public void UpdateTodo_ExistingId_PreservesCreatedAt()
    {
        var created = _service.CreateTodo(new CreateTodoRequest { Title = "Original" });

        var result = _service.UpdateTodo(created.Id, new UpdateTodoRequest { Title = "Updated" });

        Assert.Equal(created.CreatedAt.ToUniversalTime(), result!.CreatedAt.ToUniversalTime());
    }

    [Fact]
    public void UpdateTodo_NonExistingId_ReturnsNull()
    {
        var result = _service.UpdateTodo(1, new UpdateTodoRequest { Title = "Updated" });

        Assert.Null(result);
    }

    [Fact]
    public void DeleteTodo_ExistingId_DeletesAndReturnsTrue()
    {
        var created = _service.CreateTodo(new CreateTodoRequest { Title = "Delete Me" });

        var deleted = _service.DeleteTodo(created.Id);

        Assert.True(deleted);
        Assert.Null(_service.GetTodoById(created.Id));
    }

    [Fact]
    public void DeleteTodo_NonExistingId_ReturnsFalse()
    {
        var result = _service.DeleteTodo(99999);

        Assert.False(result);
    }
}
