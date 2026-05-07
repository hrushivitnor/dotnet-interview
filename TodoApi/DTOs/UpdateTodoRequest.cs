using System.ComponentModel.DataAnnotations;

namespace TodoApi.DTOs
{
    public class UpdateTodoRequest
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsCompleted { get; set; }
    }
}
