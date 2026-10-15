// Think of DbContext as EF Core's main interface for working with your database.
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) // Receives database provider and connection settings
        : base(options) // base = java super() Passes those settings to EF Core's parent DbContext
    {
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>(); // Think of it as giving your application access to the restaurant records in MSSQL. ->  _context.Restaurants 

    protected override void OnModelCreating(ModelBuilder modelBuilder) // ModelBuilder is an EF Core class used to define how your C# entities map to database tables. Configures the EF Core model
    {
        base.OnModelCreating(modelBuilder); // Call the parent DbContext class's version of OnModelCreating(). base = Java super()

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly); // Search this assembly for classes implementing IEntityTypeConfiguration<T> and apply their entity mapping rules. Loads mapping rules from RestaurantConfiguration.cs
    }
}