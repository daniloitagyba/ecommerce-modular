using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Domain;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Ordering.Application.Commands;

public sealed record PlaceOrderCommand(string CustomerEmail, List<OrderLineDto> Lines) : IRequest<Result<Guid>>;

public sealed record OrderLineDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.ProductName).NotEmpty();
            line.RuleFor(l => l.UnitPrice).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

public sealed class PlaceOrderHandler(IOrderRepository repository, IOrderingUnitOfWork unitOfWork)
    : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var lines = request.Lines.Select(l =>
            OrderLine.Create(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity));

        var result = Order.Create(request.CustomerEmail, lines);
        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        repository.Add(result.Value!);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(result.Value!.Id);
    }
}
