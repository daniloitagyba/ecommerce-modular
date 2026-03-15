using ECommerce.Modules.Ordering.Application.Commands;
using ECommerce.Modules.Ordering.Application.Queries;
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
            var id = await sender.Send(command);
            return Results.Created($"/api/orders/{id}", new { id });
        });

        group.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetOrdersQuery())));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var order = await sender.Send(new GetOrderByIdQuery(id));
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });
    }
}
