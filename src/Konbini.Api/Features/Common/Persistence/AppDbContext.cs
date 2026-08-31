using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
