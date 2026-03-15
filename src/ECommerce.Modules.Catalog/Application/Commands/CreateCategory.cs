using ECommerce.Modules.Catalog.Domain;
using ECommerce.Modules.Catalog.Infrastructure;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Commands;

public sealed record CreateCategoryCommand(string Name, string Description) : IRequest<Guid>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateCategoryHandler(CatalogDbContext db) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = Category.Create(request.Name, request.Description);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }
}
