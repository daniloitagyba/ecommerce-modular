using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Domain;
using MediatR;

namespace ECommerce.Modules.Ordering.Application.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await repository.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result<OrderDto>.Failure(OrderErrors.NotFound);

        return Result<OrderDto>.Success(new OrderDto(
            order.Id,
            order.CustomerEmail,
            order.Status.ToString(),
            order.CreatedAt,
            order.Lines.Select(l => new OrderLineResponseDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity)).ToList()));
    }
}
