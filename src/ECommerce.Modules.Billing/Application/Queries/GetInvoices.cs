using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Billing.Application.Queries;

public sealed record GetInvoicesByOrderQuery(Guid OrderId) : IRequest<List<InvoiceDto>>;

public sealed record InvoiceDto(Guid Id, string InvoiceNumber, Guid OrderId, decimal Amount, string CustomerEmail, DateTime IssuedAt);

public sealed class GetInvoicesByOrderHandler(IInvoiceRepository repository)
    : IRequestHandler<GetInvoicesByOrderQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetInvoicesByOrderQuery request, CancellationToken ct) =>
        await repository.QueryByOrderId(request.OrderId)
            .Select(i => new InvoiceDto(i.Id, i.InvoiceNumber, i.OrderId, i.Amount, i.CustomerEmail, i.IssuedAt))
            .ToListAsync(ct);
}
