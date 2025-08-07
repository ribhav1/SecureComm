using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;

namespace SecureCommAPI.Models;

public class SecureCommDbContext : DbContext
{
    public SecureCommDbContext(DbContextOptions<SecureCommDbContext> options) : base(options) { }

    public DbSet<RoomModel> Rooms { get; set; }
    public DbSet<MessageModel> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomModel>()
            .Property(r => r.ConnectedUsers)
            .HasColumnType("jsonb")
            .HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => JsonSerializer.Deserialize<Dictionary<Guid, string>>(x, (JsonSerializerOptions?)null)
            );
    }

}