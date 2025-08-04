using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
    : base(options)
    {
        //Database.EnsureCreated();   // не использовать при использовании миграций
    }

    public DbSet<Todo> TodoSet { get; set; }
}