using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Billing.Application.Queries;

public sealed record GetPaymentsByOrderQuery(Guid OrderId) : IRequest<List<PaymentDto>>;

public sealed record PaymentDto(Guid Id, Guid OrderId, decimal Amount, string Status, DateTime CreatedAt, DateTime? CompletedAt);

public sealed class GetPaymentsByOrderHandler(IPaymentRepository repository)
    : IRequestHandler<GetPaymentsByOrderQuery, List<PaymentDto>>
{
    public async Task<List<PaymentDto>> Handle(GetPaymentsByOrderQuery request, CancellationToken ct) =>
        await repository.QueryByOrderId(request.OrderId)
            .Select(p => new PaymentDto(p.Id, p.OrderId, p.Amount, p.Status.ToString(), p.CreatedAt, p.CompletedAt))
            .ToListAsync(ct);
}
