using Microsoft.AspNetCore.Mvc;
using TodoApi.DTOs;
using TodoApi.Interfaces;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("api/todos")]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var todos = _todoService.GetAllTodos();
            return Ok(todos);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var todo = _todoService.GetTodoById(id);
            return todo is null ? NotFound() : Ok(todo);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateTodoRequest request)
        {
            var todo = _todoService.CreateTodo(request);
            return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateTodoRequest request)
        {
            var todo = _todoService.UpdateTodo(id, request);
            return todo is null ? NotFound() : Ok(todo);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var deleted = _todoService.DeleteTodo(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
