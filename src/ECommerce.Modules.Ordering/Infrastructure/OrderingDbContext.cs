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
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerEmail).HasMaxLength(200).IsRequired();
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
            b.HasMany(o => o.Items).WithOne().HasForeignKey(l => l.OrderId);
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            b.Property(l => l.UnitPrice).HasPrecision(18, 2);
            b.Ignore(l => l.Total);
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(o => o.Id);
            b.Property(o => o.Type).HasMaxLength(500).IsRequired();
            b.Property(o => o.Content).IsRequired();
            b.HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await this.DispatchDomainEventsAsync(publisher, cancellationToken);
        return result;
    }
}
