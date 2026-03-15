using ECommerce.Modules.Ordering.Application.Commands;
using ECommerce.Modules.Ordering.Application.Queries;
using ECommerce.Shared.Application;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Modules.Ordering.Endpoints;

public static class OrderingEndpointMapper
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders").WithTags("Ordering");

        group.MapPost("/", async (PlaceOrderCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/", async (int? page, int? pageSize, ISender sender) =>
        {
            var paging = new PagedRequest(page ?? 1, pageSize ?? 20);
            return Results.Ok(await sender.Send(new GetOrdersQuery(paging)));
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });
    }
}
