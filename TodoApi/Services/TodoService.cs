using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using TodoApi.DTOs;
using TodoApi.Interfaces;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class TodoService : ITodoService
    {
        private readonly string _connectionString;

        public TodoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

 
        public TodoService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Todo CreateTodo(CreateTodoRequest request)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Todos (Title, Description, IsCompleted, CreatedAt)
                VALUES (@Title, @Description, @IsCompleted, @CreatedAt);
                SELECT last_insert_rowid();
            ";
            var createdAt = DateTime.UtcNow;
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@Description", request.Description ?? string.Empty);
            command.Parameters.AddWithValue("@IsCompleted", request.IsCompleted ? 1 : 0);
            command.Parameters.AddWithValue("@CreatedAt", createdAt.ToString("o"));

            var id = Convert.ToInt32(command.ExecuteScalar());

            return new Todo
            {
                Id = id,
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                IsCompleted = request.IsCompleted,
                CreatedAt = createdAt
            };
        }

        public IEnumerable<Todo> GetAllTodos()
        {
            var todos = new List<Todo>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Todos";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                todos.Add(new Todo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    IsCompleted = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind)
                });
            }

            return todos;
        }

        public Todo? GetTodoById(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Todos WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Todo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    IsCompleted = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind)
                };
            }

            return null;
        }

        public Todo? UpdateTodo(int id, UpdateTodoRequest request)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Todos
                SET Title = @Title, Description = @Description, IsCompleted = @IsCompleted
                WHERE Id = @Id
            ";
            updateCmd.Parameters.AddWithValue("@Title", request.Title);
            updateCmd.Parameters.AddWithValue("@Description", request.Description ?? string.Empty);
            updateCmd.Parameters.AddWithValue("@IsCompleted", request.IsCompleted ? 1 : 0);
            updateCmd.Parameters.AddWithValue("@Id", id);

            var rowsAffected = updateCmd.ExecuteNonQuery();
            if (rowsAffected == 0) return null;

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Todos WHERE Id = @Id";
            selectCmd.Parameters.AddWithValue("@Id", id);

            using var reader = selectCmd.ExecuteReader();
            if (reader.Read())
            {
                return new Todo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    IsCompleted = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind)
                };
            }

            return null;
        }

        public bool DeleteTodo(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Todos WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }
    }
}
