namespace TodoApi.Models;

public class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }

    public string? UserId { get; set; }
    public ApiUser? User { get; set; }
}
