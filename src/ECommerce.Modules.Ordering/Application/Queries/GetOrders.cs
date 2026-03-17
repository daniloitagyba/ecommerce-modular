using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Application;
using MediatR;

namespace ECommerce.Modules.Ordering.Application.Queries;

public sealed record GetOrdersQuery(PagedRequest Paging) : IRequest<PagedResult<OrderDto>>;

public sealed record OrderDto(Guid Id, string CustomerEmail, string Status, DateTime CreatedAt, decimal TotalAmount, List<OrderItemResponseDto> Items);

public sealed record OrderItemResponseDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed class GetOrdersHandler(IOrderRepository repository)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct) =>
        await repository.QueryWithItems()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerEmail,
                o.Status.ToString(),
                o.CreatedAt,
                o.TotalAmount,
                o.Items.Select(l => new OrderItemResponseDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity)).ToList()))
            .ToPagedResultAsync(request.Paging, ct);
}
