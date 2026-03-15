using ECommerce.Modules.Catalog.Application.Commands;
using ECommerce.Modules.Catalog.Application.Queries;
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
            var id = await sender.Send(command);
            return Results.Created($"/api/catalog/categories/{id}", new { id });
        });

        group.MapGet("/products", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetProductsQuery())));

        group.MapGet("/products/{id:guid}", async (Guid id, ISender sender) =>
        {
            var product = await sender.Send(new GetProductByIdQuery(id));
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });

        group.MapPost("/products", async (CreateProductCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/catalog/products/{id}", new { id });
        });

        group.MapPut("/products/{id:guid}/stock", async (Guid id, UpdateStockRequest request, ISender sender) =>
        {
            await sender.Send(new UpdateStockCommand(id, request.Quantity));
            return Results.NoContent();
        });
    }
}

public sealed record UpdateStockRequest(int Quantity);
