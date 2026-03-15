using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Domain;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Commands;

public sealed record UpdateStockCommand(Guid ProductId, int Quantity) : IRequest<Result>;

public sealed class UpdateStockValidator : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class UpdateStockHandler(IProductRepository repository, ICatalogUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStockCommand, Result>
{
    public async Task<Result> Handle(UpdateStockCommand request, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        if (request.Quantity > 0)
            product.IncreaseStock(request.Quantity);
        else
        {
            var decreaseResult = product.DecreaseStock(Math.Abs(request.Quantity));
            if (decreaseResult.IsFailure)
                return decreaseResult;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
