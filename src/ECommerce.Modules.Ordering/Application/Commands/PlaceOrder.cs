using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Domain;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Ordering.Application.Commands;

public sealed record PlaceOrderCommand(string CustomerEmail, List<OrderItemRequest> Items) : IRequest<Result<Guid>>;

public sealed record OrderItemRequest(Guid ProductId, int Quantity);

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(l => l.ProductId).NotEmpty();
            item.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

public sealed class PlaceOrderHandler(
    IOrderRepository repository,
    IOrderingUnitOfWork unitOfWork,
    IProductChecker productChecker)
    : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var productItems = request.Items
            .Select(l => new ProductLineRequest(l.ProductId, l.Quantity))
            .ToList();

        var validation = await productChecker.ValidateProductsAsync(productItems, ct);
        if (validation.IsFailure)
            return Result<Guid>.Failure(validation.Error);

        var items = validation.Value
            .Select(v => OrderItem.Create(v.ProductId, v.ProductName, v.UnitPrice, v.Quantity));

        var result = Order.Create(request.CustomerEmail, items);
        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        repository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(result.Value.Id);
    }
}
