using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Tests.EndToEnd;

public class BillingEndpointTests(ECommerceWebAppFactory factory) : IClassFixture<ECommerceWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetPayments_ShouldReturnEmpty_WhenNoOrder()
    {
        var response = await _client.GetAsync($"/api/billing/payments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<object>>();
        payments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInvoices_ShouldReturnEmpty_WhenNoOrder()
    {
        var response = await _client.GetAsync($"/api/billing/invoices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoices = await response.Content.ReadFromJsonAsync<List<object>>();
        invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task PlaceOrder_ShouldAutomaticallyCreatePaymentAndInvoice()
    {
        // Place an order
        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "billing@test.com",
            lines = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Keyboard", unitPrice = 150.0, quantity = 2 }
            }
        });
        var orderBody = await orderResponse.Content.ReadFromJsonAsync<IdResponse>();
        var orderId = orderBody!.Id;

        // Check payment was created
        var paymentResponse = await _client.GetAsync($"/api/billing/payments/{orderId}");
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await paymentResponse.Content.ReadFromJsonAsync<List<PaymentResponse>>();
        payments.Should().ContainSingle()
            .Which.Should().Match<PaymentResponse>(p =>
                p.OrderId == orderId &&
                p.Amount == 300.0m &&
                p.Status == "Completed");

        // Check invoice was created
        var invoiceResponse = await _client.GetAsync($"/api/billing/invoices/{orderId}");
        invoiceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoices = await invoiceResponse.Content.ReadFromJsonAsync<List<InvoiceResponse>>();
        invoices.Should().ContainSingle()
            .Which.Should().Match<InvoiceResponse>(i =>
                i.OrderId == orderId &&
                i.Amount == 300.0m &&
                i.CustomerEmail == "billing@test.com" &&
                i.InvoiceNumber.StartsWith("INV-"));
    }

    [Fact]
    public async Task FullFlow_CreateProduct_PlaceOrder_VerifyBilling()
    {
        // 1. Create category
        var catResponse = await _client.PostAsJsonAsync("/api/catalog/categories",
            new { name = "E2E Test", description = "Full flow test" });
        var catBody = await catResponse.Content.ReadFromJsonAsync<IdResponse>();

        // 2. Create product
        var prodResponse = await _client.PostAsJsonAsync("/api/catalog/products",
            new { name = "Monitor", sku = $"MON-{Guid.NewGuid():N}"[..20], price = 500.0, stockQuantity = 10, categoryId = catBody!.Id });
        var prodBody = await prodResponse.Content.ReadFromJsonAsync<IdResponse>();
        prodResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Place order
        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "fullflow@test.com",
            lines = new[]
            {
                new { productId = prodBody!.Id, productName = "Monitor", unitPrice = 500.0, quantity = 2 }
            }
        });
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderBody = await orderResponse.Content.ReadFromJsonAsync<IdResponse>();

        // 4. Verify order
        var getOrder = await _client.GetAsync($"/api/orders/{orderBody!.Id}");
        getOrder.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify payment
        var payments = await (await _client.GetAsync($"/api/billing/payments/{orderBody.Id}"))
            .Content.ReadFromJsonAsync<List<PaymentResponse>>();
        payments.Should().ContainSingle().Which.Amount.Should().Be(1000m);

        // 6. Verify invoice
        var invoices = await (await _client.GetAsync($"/api/billing/invoices/{orderBody.Id}"))
            .Content.ReadFromJsonAsync<List<InvoiceResponse>>();
        invoices.Should().ContainSingle().Which.Amount.Should().Be(1000m);
    }
}

public record PaymentResponse(Guid Id, Guid OrderId, decimal Amount, string Status, DateTime CreatedAt, DateTime? CompletedAt);
public record InvoiceResponse(Guid Id, string InvoiceNumber, Guid OrderId, decimal Amount, string CustomerEmail, DateTime IssuedAt);
