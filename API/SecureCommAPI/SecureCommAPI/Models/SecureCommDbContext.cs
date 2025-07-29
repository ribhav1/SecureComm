using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace SecureCommAPI.Models;

public class SecureCommDbContext : DbContext
{
    public SecureCommDbContext(DbContextOptions<SecureCommDbContext> options) : base(options) { }

    public DbSet<RoomModel> Rooms { get; set; }
    public DbSet<MessageModel> Messages { get; set; }
}