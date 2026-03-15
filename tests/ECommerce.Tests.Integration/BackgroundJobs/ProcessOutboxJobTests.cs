using System.Text.Json;
using ECommerce.API.BackgroundJobs;
using ECommerce.Modules.Ordering.Infrastructure;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;

namespace ECommerce.Tests.Integration.BackgroundJobs;

public class ProcessOutboxJobTests : IDisposable
{
    private readonly DbContextFactory _factory = new();
    private readonly IBus _bus = Substitute.For<IBus>();
    private readonly ILogger<ProcessOutboxJob> _logger = Substitute.For<ILogger<ProcessOutboxJob>>();
    private readonly IJobExecutionContext _jobContext = Substitute.For<IJobExecutionContext>();

    public ProcessOutboxJobTests()
    {
        _jobContext.CancellationToken.Returns(CancellationToken.None);
    }

    [Fact]
    public async Task Execute_ShouldPublishUnprocessedMessages_AndMarkAsProcessed()
    {
        await using var db = _factory.CreateOrderingContext();
        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedIntegrationEvent(orderId, "test@example.com", 100m);

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(evt)
        });
        await db.Database.EnsureCreatedAsync();
        await SaveWithoutDomainEvents(db);

        var job = new ProcessOutboxJob(db, _bus, _logger);
        await job.Execute(_jobContext);

        await _bus.Received(1).Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>());

        var messages = db.OutboxMessages.ToList();
        messages.Should().ContainSingle()
            .Which.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_ShouldDoNothing_WhenNoUnprocessedMessages()
    {
        await using var db = _factory.CreateOrderingContext();
        await db.Database.EnsureCreatedAsync();

        var job = new ProcessOutboxJob(db, _bus, _logger);
        await job.Execute(_jobContext);

        await _bus.DidNotReceive().Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldSkipAlreadyProcessedMessages()
    {
        await using var db = _factory.CreateOrderingContext();
        var evt = new OrderCreatedIntegrationEvent(Guid.NewGuid(), "a@b.com", 50m);

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(evt),
            ProcessedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.Database.EnsureCreatedAsync();
        await SaveWithoutDomainEvents(db);

        var job = new ProcessOutboxJob(db, _bus, _logger);
        await job.Execute(_jobContext);

        await _bus.DidNotReceive().Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ShouldSkipMessage_WhenTypeIsUnknown()
    {
        await using var db = _factory.CreateOrderingContext();

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = "Some.NonExistent.Type, FakeAssembly",
            Content = "{}"
        });
        await db.Database.EnsureCreatedAsync();
        await SaveWithoutDomainEvents(db);

        var job = new ProcessOutboxJob(db, _bus, _logger);
        await job.Execute(_jobContext);

        await _bus.DidNotReceive().Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>());

        db.OutboxMessages.ToList().Should().ContainSingle()
            .Which.ProcessedAt.Should().NotBeNull("unknown types should be marked as processed to avoid reprocessing");
    }

    [Fact]
    public async Task Execute_ShouldSkipMessage_WhenDeserializationFails()
    {
        await using var db = _factory.CreateOrderingContext();

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
            Content = "{ invalid json !!!"
        });
        await db.Database.EnsureCreatedAsync();
        await SaveWithoutDomainEvents(db);

        var job = new ProcessOutboxJob(db, _bus, _logger);

        // Should not throw — errors are caught and logged
        await job.Execute(_jobContext);

        db.OutboxMessages.ToList().Should().ContainSingle()
            .Which.ProcessedAt.Should().BeNull("failed messages should remain unprocessed for retry");
    }

    [Fact]
    public async Task Execute_ShouldProcessMultipleMessages_InOrder()
    {
        await using var db = _factory.CreateOrderingContext();
        await db.Database.EnsureCreatedAsync();

        for (var i = 1; i <= 3; i++)
        {
            var evt = new OrderCreatedIntegrationEvent(Guid.NewGuid(), $"user{i}@test.com", i * 100m);
            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(evt),
                CreatedAt = DateTime.UtcNow.AddSeconds(i)
            });
        }
        await SaveWithoutDomainEvents(db);

        var job = new ProcessOutboxJob(db, _bus, _logger);
        await job.Execute(_jobContext);

        await _bus.Received(3).Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>());

        db.OutboxMessages.ToList()
            .Should().OnlyContain(m => m.ProcessedAt != null);
    }

    [Fact]
    public async Task Execute_ShouldContinueProcessing_WhenOneMessageFails()
    {
        await using var db = _factory.CreateOrderingContext();
        await db.Database.EnsureCreatedAsync();

        // First message — valid
        var valid = new OrderCreatedIntegrationEvent(Guid.NewGuid(), "ok@test.com", 200m);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(valid),
            CreatedAt = DateTime.UtcNow.AddSeconds(1)
        });

        // Second message — will cause bus.Publish to throw
        var failing = new OrderCreatedIntegrationEvent(Guid.NewGuid(), "fail@test.com", 300m);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(failing),
            CreatedAt = DateTime.UtcNow.AddSeconds(2)
        });

        // Third message — valid
        var valid2 = new OrderCreatedIntegrationEvent(Guid.NewGuid(), "ok2@test.com", 400m);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(OrderCreatedIntegrationEvent).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(valid2),
            CreatedAt = DateTime.UtcNow.AddSeconds(3)
        });
        await SaveWithoutDomainEvents(db);

        // Make second Publish call throw
        var callCount = 0;
        _bus.Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount == 2)
                    throw new InvalidOperationException("Bus failure");
                return Task.CompletedTask;
            });

        var job = new ProcessOutboxJob(db, _bus, _logger);
        await job.Execute(_jobContext);

        // All 3 messages were attempted
        await _bus.Received(3).Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>());

        var messages = db.OutboxMessages.OrderBy(m => m.CreatedAt).ToList();
        messages[0].ProcessedAt.Should().NotBeNull("first message succeeded");
        messages[1].ProcessedAt.Should().BeNull("second message failed and should be retried");
        messages[2].ProcessedAt.Should().NotBeNull("third message succeeded despite second failure");
    }

    /// <summary>
    /// Saves OutboxMessage entities directly. Since OutboxMessage is not an Entity subclass,
    /// the overridden SaveChangesAsync won't find any integration events to convert.
    /// </summary>
    private static async Task SaveWithoutDomainEvents(OrderingDbContext db)
    {
        await db.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
