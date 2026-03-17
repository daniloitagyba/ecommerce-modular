using ECommerce.Shared.Domain;

namespace ECommerce.Tests.Unit.Shared;

public class EntityTests
{
    private sealed class TestEntity : Entity;
    private sealed record TestEvent(string Message) : IDomainEvent;

    [Fact]
    public void NewEntity_ShouldHaveGeneratedId()
    {
        var entity = new TestEntity();

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AddDomainEvent_ShouldStoreEvent()
    {
        var entity = new TestEntity();
        var evt = new TestEvent("test");

        entity.AddDomainEvent(evt);

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(evt);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestEvent("1"));
        entity.AddDomainEvent(new TestEvent("2"));

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldReturnReadOnlyList()
    {
        var entity = new TestEntity();

        entity.DomainEvents.Should().BeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }
}
