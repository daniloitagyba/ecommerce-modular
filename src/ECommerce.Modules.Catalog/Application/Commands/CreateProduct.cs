using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Domain;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Commands;

public sealed record CreateProductCommand(string Name, string Sku, decimal Price, int StockQuantity, Guid CategoryId)
    : IRequest<Result<Guid>>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public sealed class CreateProductHandler(IProductRepository repository, ICatalogUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var result = Product.Create(request.Name, request.Sku, request.Price, request.StockQuantity, request.CategoryId);
        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        repository.Add(result.Value!);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(result.Value!.Id);
    }
}
