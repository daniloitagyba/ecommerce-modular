using System.Text.Json;
using ECommerce.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Modules.Ordering.Infrastructure;

public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            ConvertIntegrationEventsToOutboxMessages(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            ConvertIntegrationEventsToOutboxMessages(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private static void ConvertIntegrationEventsToOutboxMessages(DbContext context)
    {
        var outboxSet = context.Set<OutboxMessage>();

        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any(d => d is IIntegrationEvent))
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            var integrationEvents = entity.DomainEvents.OfType<IIntegrationEvent>().ToList();
            foreach (var evt in integrationEvents)
            {
                outboxSet.Add(new OutboxMessage
                {
                    Type = evt.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(evt, evt.GetType())
                });
            }
        }
    }
}
