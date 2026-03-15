using ECommerce.Modules.Catalog.Application.Commands;
using ECommerce.Modules.Catalog.Application.Queries;
using ECommerce.Shared.Application;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Modules.Catalog.Endpoints;

public static class CatalogEndpointMapper
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/catalog").WithTags("Catalog");

        group.MapPost("/categories", async (CreateCategoryCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/categories/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/products", async (int? page, int? pageSize, ISender sender) =>
        {
            var paging = new PagedRequest(page ?? 1, pageSize ?? 20);
            return Results.Ok(await sender.Send(new GetProductsQuery(paging)));
        });

        group.MapGet("/products/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/products", async (CreateProductCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/products/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPut("/products/{id:guid}/stock", async (Guid id, UpdateStockRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateStockCommand(id, request.Quantity));
            return result.IsSuccess ? Results.NoContent() : Results.UnprocessableEntity(new { error = result.Error });
        });
    }
}

public sealed record UpdateStockRequest(int Quantity);
