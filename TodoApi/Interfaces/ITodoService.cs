using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Interfaces
{
    public interface ITodoService
    {
        Todo CreateTodo(CreateTodoRequest request);
        IEnumerable<Todo> GetAllTodos();
        Todo? GetTodoById(int id);
        Todo? UpdateTodo(int id, UpdateTodoRequest request);
        bool DeleteTodo(int id);
    }
}
