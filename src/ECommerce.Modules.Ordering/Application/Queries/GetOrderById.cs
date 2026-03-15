using ECommerce.Modules.Ordering.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Ordering.Application.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

public sealed class GetOrderByIdHandler(OrderingDbContext db) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken ct) =>
        await db.Orders
            .Include(o => o.Lines)
            .Where(o => o.Id == request.Id)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerEmail,
                o.Status.ToString(),
                o.CreatedAt,
                o.Lines.Select(l => new OrderLineResponseDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity)).ToList()))
            .FirstOrDefaultAsync(ct);
}
