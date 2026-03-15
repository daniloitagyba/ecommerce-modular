using ECommerce.Modules.Catalog.Domain;
using ECommerce.Modules.Catalog.Infrastructure;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Commands;

public sealed record CreateProductCommand(string Name, string Sku, decimal Price, int StockQuantity, Guid CategoryId) : IRequest<Guid>;

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

public sealed class CreateProductHandler(CatalogDbContext db) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(request.Name, request.Sku, request.Price, request.StockQuantity, request.CategoryId);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }
}
