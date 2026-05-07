# SOLUTION.md

## Problems Identified

### 1. SQL Injection (Critical)
Every query used string interpolation to build SQL commands:

An attacker could pass `'; DROP TABLE Todos;--` as a title and destroy the database.  
**Fix:** All queries now use parameterized commands (`@Id`, `@Title`, etc.).

### 2. No Dependency Injection
`TodoController` called `new TodoService()` inside every action method, creating tight coupling and making the controller impossible to unit test without a real database.  
**Fix:** `ITodoService` registered in the DI container as `AddScoped`; injected into the controller via constructor.

### 3. Non-RESTful API — all operations used POST
`POST /api/getTodo`, `POST /api/deleteTodo` etc. violate HTTP semantics and break caching, idempotency, and conventional client expectations.  
**Fix:**

| Before | After |
|--------|-------|
| `POST /api/getTodo` (all) | `GET /api/todos` |
| `POST /api/getTodo` (by id) | `GET /api/todos/{id}` |
| `POST /api/createTodo` | `POST /api/todos` |
| `POST /api/updateTodo` | `PUT /api/todos/{id}` |
| `POST /api/deleteTodo` | `DELETE /api/todos/{id}` |

### 4. No Service Interface
Without `ITodoService`, controller tests were impossible to write with mocked dependencies.  
**Fix:** `ITodoService` introduced in `Interfaces/`.

### 5. Hard-coded Connection String — duplicated in two places
`"Data Source=todos.db"` appeared in both `Program.cs` and `TodoService.cs`. Changing it required editing source code in two files.  
**Fix:** Defined once in `appsettings.json` under `ConnectionStrings:DefaultConnection`, read via `IConfiguration`.

### 6. DTOs Defined Inside the Controller File
`GetTodoRequest`, `UpdateTodoRequest`, `DeleteTodoRequest` were all declared at the bottom of `TodoController.cs`.  
**Fix:** Moved to a dedicated `DTOs/` folder (`CreateTodoRequest.cs`, `UpdateTodoRequest.cs`).

### 7. Overloaded GET Endpoint
`POST /api/getTodo` returned either a single item or all items depending on whether `Id` was in the request body — two completely different behaviours in one endpoint.  
**Fix:** Split into `GET /api/todos` and `GET /api/todos/{id}`.

### 8. No Input Validation
The model had no `[Required]` or `[MaxLength]` attributes. A missing title reached the database before anything failed, returning a raw unstructured error.  
**Fix:** DTOs use `[Required]` and `[MaxLength]`; ASP.NET Core's model validation pipeline returns a structured `400 Bad Request` automatically.

### 9. `UpdateTodo` Didn't Check If the Row Existed
`rowsAffected` was captured from the UPDATE but never checked. Updating a non-existent ID silently returned `200 OK` with whatever data was in the request.  
**Fix:** If `rowsAffected == 0`, return `null`; controller maps that to `404 Not Found`.

### 10. `UpdateTodo` Returned a Todo Missing `CreatedAt`
The response was built from the request body, so `CreatedAt` was always `DateTime.MinValue` in the response.  
**Fix:** After a successful update, a `SELECT` fetches the full record from the database and returns it.

### 11. Nullable Reference
`Todo.Title` and `Todo.Description` were non-nullable strings with no initialiser, generating compiler warnings under `<Nullable>enable</Nullable>`.  
**Fix:** Both properties initialised to `string.Empty`.

### 12. Tests Hit a Real Shared Database File
All original tests wrote to the actual `todos.db` file on disk and shared state, so execution order determined pass/fail.  
**Fix:** Service tests use SQLite's named in-memory databases (`Mode=Memory;Cache=Shared`) with a unique name per test instance. A keep-alive connection holds the database open; `IDisposable` tears it down. Each test starts with a completely empty database.

### 13. Tests Were Meaningless
- `Test1()` asserted `Assert.True(true)`
- `TestGetTodo` asserted `Count > 0`, relying on `TestCreateTodo` leaving data behind
- `UpdateTest` hard-coded `Id = 1`
- No negative test cases anywhere

**Fix:** Replaced with specific, isolated, named cases covering both positive and negative paths.

---

## Architectural Decisions

**Layered separation:** The controller handles HTTP concerns only (routing, status codes, request/response mapping). `TodoService` owns all data access logic. This keeps each layer independently testable and changeable.

**Raw ADO.NET kept:** The original code used `Microsoft.Data.Sqlite` directly. Switching to EF Core was out of scope for a refactor — the goal was to fix the existing code, not replace it. Parameterized queries make it safe.

**Scoped service lifetime:** `AddScoped<ITodoService, TodoService>()` — one service instance per HTTP request, which matches the per-operation connection pattern already in use.

**Two-constructor `TodoService`:** One takes `IConfiguration` (used by DI in production); one takes a plain connection string (used directly in service integration tests). This avoids pulling extra configuration packages into the test project.

**`DbInitializer` extracted to its own class:** Database setup logic was sitting as a local function in `Program.cs`. Moving it to `Data/DbInitializer.cs` keeps `Program.cs` focused on application wiring.

---

## How to Run

### Prerequisites
- .NET 8 SDK

### Run the API
```bash
dotnet run --project TodoApi
```
Swagger UI: `https://localhost:{port}/swagger`

### Run Tests
```bash
# All tests
dotnet test

# Verbose output
dotnet test --logger "console;verbosity=detailed"

# Single test by name
dotnet test TodoApi.Tests --filter "FullyQualifiedName~DeleteTodo_ExistingId"
```

---

## API Documentation

Base URL: `https://localhost:{port}/api/todos`

### GET /api/todos
Returns all todo items.
- **200 OK** — array of todos (empty array if none)

### GET /api/todos/{id}
Returns a single todo.
- **200 OK** — todo object
- **404 Not Found**

### POST /api/todos
Creates a new todo.

Request body:
```json
{
  "title": "Buy groceries",
  "description": "Milk, eggs, bread",
  "isCompleted": false
}
```
- `title` — required, max 200 chars
- `description` — optional, max 1000 chars
- `isCompleted` — optional, defaults to `false`

Responses:
- **201 Created** — created todo; `Location` header points to `GET /api/todos/{id}`
- **400 Bad Request** — validation error with field-level details

### PUT /api/todos/{id}
Replaces an existing todo. Same request body shape as POST.
- **200 OK** — updated todo (includes original `createdAt`)
- **400 Bad Request** — validation error
- **404 Not Found**

### DELETE /api/todos/{id}
Deletes a todo.
- **204 No Content** — deleted
- **404 Not Found**

---

## Future Improvements

1. **Entity Framework Core** — Replace raw ADO.NET with EF Core for proper migrations, change tracking, and LINQ queries.

2. **Pagination** — `GET /api/todos` returns all records. Add `?page=1&pageSize=20` query parameters for large datasets.

3. **Global Exception Handling** — Middleware to catch unhandled exceptions and return structured `ProblemDetails` responses instead of raw 500s.

4. **Logging** — Inject `ILogger<T>` into the service and controller to trace operations and errors through the standard logging pipeline.

5. **Authentication** — Protect write endpoints (`POST`, `PUT`, `DELETE`) with JWT bearer authentication.

6. **Integration Tests with WebApplicationFactory** — A test layer that boots the full ASP.NET Core pipeline against an in-memory SQLite database to cover routing, model validation, and middleware end-to-end.

7. **Response DTOs** — Introduce a `TodoResponse` DTO to decouple the internal domain model from the API contract, so internal changes don't break the public API shape.
