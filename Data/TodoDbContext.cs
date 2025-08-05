using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoDbContext : IdentityDbContext<ApiUser>
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
    : base(options)
    {
        //Database.EnsureCreated();   // не использовать при использовании миграций
    }

    public DbSet<Todo> TodoSet { get; set; }
}