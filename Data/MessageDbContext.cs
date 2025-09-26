using Microsoft.EntityFrameworkCore;
using qqbot.Models.Entities;

namespace qqbot.Data;

public class MessageDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
