namespace ECommerce.Shared.Domain;

public sealed record OrderCreatedIntegrationEvent(Guid OrderId, string CustomerEmail, decimal TotalAmount) : IDomainEvent;
