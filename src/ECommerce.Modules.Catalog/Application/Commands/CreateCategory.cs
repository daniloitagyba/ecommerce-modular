using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Domain;
using FluentValidation;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Commands;

public sealed record CreateCategoryCommand(string Name, string Description) : IRequest<Result<Guid>>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateCategoryHandler(ICategoryRepository repository, ICatalogUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var result = Category.Create(request.Name, request.Description);
        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        repository.Add(result.Value!);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(result.Value!.Id);
    }
}
