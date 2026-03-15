using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Domain;
using ECommerce.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Ordering.Infrastructure;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options, IPublisher publisher)
    : DbContext(options), IOrderingUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerEmail).HasMaxLength(200).IsRequired();
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
            b.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
            b.Ignore(o => o.TotalAmount);
        });

        modelBuilder.Entity<OrderLine>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            b.Property(l => l.UnitPrice).HasPrecision(18, 2);
            b.Ignore(l => l.Total);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await this.DispatchDomainEventsAsync(publisher, cancellationToken);
        return result;
    }
}
