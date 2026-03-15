using ECommerce.Modules.Catalog.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Catalog.Application.Commands;

public sealed record UpdateStockCommand(Guid ProductId, int Quantity) : IRequest;

public sealed class UpdateStockValidator : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class UpdateStockHandler(CatalogDbContext db) : IRequestHandler<UpdateStockCommand>
{
    public async Task Handle(UpdateStockCommand request, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} not found.");

        if (request.Quantity > 0)
            product.IncreaseStock(request.Quantity);
        else
            product.DecreaseStock(Math.Abs(request.Quantity));

        await db.SaveChangesAsync(ct);
    }
}
