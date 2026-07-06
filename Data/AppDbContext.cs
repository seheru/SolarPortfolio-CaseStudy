using Microsoft.EntityFrameworkCore;
using SolarBackend.Models;

namespace SolarBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
}
