using ECommerce.Modules.Billing.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Modules.Billing.Endpoints;

public static class BillingEndpointMapper
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/billing").WithTags("Billing");

        group.MapGet("/payments/{orderId:guid}", async (Guid orderId, ISender sender) =>
            Results.Ok(await sender.Send(new GetPaymentsByOrderQuery(orderId))));

        group.MapGet("/invoices/{orderId:guid}", async (Guid orderId, ISender sender) =>
            Results.Ok(await sender.Send(new GetInvoicesByOrderQuery(orderId))));
    }
}
