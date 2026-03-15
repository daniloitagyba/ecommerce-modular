using ECommerce.Modules.Ordering.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Ordering.Application.Queries;

public sealed record GetOrdersQuery : IRequest<List<OrderDto>>;

public sealed record OrderDto(Guid Id, string CustomerEmail, string Status, DateTime CreatedAt, List<OrderLineResponseDto> Lines);

public sealed record OrderLineResponseDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed class GetOrdersHandler(OrderingDbContext db) : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct) =>
        await db.Orders
            .Include(o => o.Lines)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerEmail,
                o.Status.ToString(),
                o.CreatedAt,
                o.Lines.Select(l => new OrderLineResponseDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity)).ToList()))
            .ToListAsync(ct);
}
