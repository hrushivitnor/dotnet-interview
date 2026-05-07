using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.DTOs;
using TodoApi.Interfaces;
using TodoApi.Models;

namespace TodoApi.Tests;

public class TodoControllerTests
{
    private readonly Mock<ITodoService> _mockService;
    private readonly TodoController _controller;

    public TodoControllerTests()
    {
        _mockService = new Mock<ITodoService>();
        _controller = new TodoController(_mockService.Object);
    }

    [Fact]
    public void GetAll_ReturnsOkWithAllTodos()
    {
        var todos = new List<Todo>
        {
            new() { Id = 1, Title = "Task 1" },
            new() { Id = 2, Title = "Task 2" }
        };
        _mockService.Setup(s => s.GetAllTodos()).Returns(todos);

        var result = _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(todos, ok.Value);
    }

    [Fact]
    public void GetById_ExistingId_ReturnsOkWithTodo()
    {
        var todo = new Todo { Id = 1, Title = "Task 1" };
        _mockService.Setup(s => s.GetTodoById(1)).Returns(todo);

        var result = _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(todo, ok.Value);
    }

    [Fact]
    public void GetById_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetTodoById(999)).Returns((Todo?)null);

        var result = _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_ValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateTodoRequest { Title = "New Task" };
        var todo = new Todo { Id = 1, Title = "New Task", CreatedAt = DateTime.UtcNow };
        _mockService.Setup(s => s.CreateTodo(request)).Returns(todo);

        var result = _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), created.ActionName);
        Assert.Equal(todo, created.Value);
    }

    [Fact]
    public void Update_ExistingId_ReturnsOkWithUpdatedTodo()
    {
        var request = new UpdateTodoRequest { Title = "Updated Task", IsCompleted = true };
        var updated = new Todo { Id = 1, Title = "Updated Task", IsCompleted = true };
        _mockService.Setup(s => s.UpdateTodo(1, request)).Returns(updated);

        var result = _controller.Update(1, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public void Update_NonExistingId_ReturnsNotFound()
    {
        var request = new UpdateTodoRequest { Title = "Updated Task" };
        _mockService.Setup(s => s.UpdateTodo(999, request)).Returns((Todo?)null);

        var result = _controller.Update(999, request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Delete_ExistingId_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteTodo(1)).Returns(true);

        var result = _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Delete_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteTodo(999)).Returns(false);

        var result = _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
