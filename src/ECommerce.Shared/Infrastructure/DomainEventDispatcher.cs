using ECommerce.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shared.Infrastructure;

public static class DomainEventDispatcher
{
    public static async Task DispatchDomainEventsAsync(this DbContext context, IPublisher publisher, CancellationToken ct = default)
    {
        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .Where(e => e is not IIntegrationEvent) // Integration events go to outbox
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, ct);
        }
    }
}
