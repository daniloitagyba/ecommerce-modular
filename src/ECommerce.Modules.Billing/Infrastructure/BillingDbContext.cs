using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Domain;
using ECommerce.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Billing.Infrastructure;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options, IPublisher publisher)
    : DbContext(options), IBillingUnitOfWork
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("billing");

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Amount).HasPrecision(18, 2);
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
            b.HasIndex(p => p.OrderId);
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
            b.HasIndex(i => i.InvoiceNumber).IsUnique();
            b.Property(i => i.CustomerEmail).HasMaxLength(200).IsRequired();
            b.Property(i => i.Amount).HasPrecision(18, 2);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await this.DispatchDomainEventsAsync(publisher, cancellationToken);
        return result;
    }
}
