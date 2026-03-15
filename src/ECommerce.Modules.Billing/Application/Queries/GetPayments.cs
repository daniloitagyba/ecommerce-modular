using ECommerce.Modules.Billing.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Billing.Application.Queries;

public sealed record GetPaymentsByOrderQuery(Guid OrderId) : IRequest<List<PaymentDto>>;

public sealed record PaymentDto(Guid Id, Guid OrderId, decimal Amount, string Status, DateTime CreatedAt, DateTime? CompletedAt);

public sealed class GetPaymentsByOrderHandler(BillingDbContext db) : IRequestHandler<GetPaymentsByOrderQuery, List<PaymentDto>>
{
    public async Task<List<PaymentDto>> Handle(GetPaymentsByOrderQuery request, CancellationToken ct) =>
        await db.Payments
            .Where(p => p.OrderId == request.OrderId)
            .Select(p => new PaymentDto(p.Id, p.OrderId, p.Amount, p.Status.ToString(), p.CreatedAt, p.CompletedAt))
            .ToListAsync(ct);
}
