using System.Text.Json;
using ECommerce.Modules.Ordering.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace ECommerce.API.BackgroundJobs;

/// <summary>
/// Quartz.NET job that reads unprocessed outbox messages from the database
/// and publishes them to MassTransit for async processing by consumers.
/// This implements the Transactional Outbox Pattern: the order transaction
/// and outbox insert are ACID, while billing processing is eventually consistent.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ProcessOutboxJob(
    OrderingDbContext dbContext,
    IBus bus,
    ILogger<ProcessOutboxJob> logger) : IJob
{
    private const int BatchSize = 20;

    public async Task Execute(IJobExecutionContext context)
    {
        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
            return;

        logger.LogInformation("Processing {Count} outbox message(s)", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType is null)
                {
                    logger.LogWarning("Unknown event type: {Type}. Skipping message {Id}", message.Type, message.Id);
                    message.ProcessedAt = DateTime.UtcNow;
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Content, eventType);
                if (@event is null)
                {
                    logger.LogWarning("Failed to deserialize message {Id}. Skipping", message.Id);
                    message.ProcessedAt = DateTime.UtcNow;
                    continue;
                }

                await bus.Publish(@event, eventType, context.CancellationToken);
                message.ProcessedAt = DateTime.UtcNow;

                logger.LogInformation("Published outbox message {Id} of type {Type}", message.Id, eventType.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
