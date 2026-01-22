using Microsoft.EntityFrameworkCore;
using SagaDemo.Models;

namespace SagaDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<SagaInstance> SagaInstances => Set<SagaInstance>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}
