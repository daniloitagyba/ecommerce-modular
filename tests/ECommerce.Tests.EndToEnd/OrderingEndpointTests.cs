using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Tests.EndToEnd;

public class OrderingEndpointTests(ECommerceWebAppFactory factory) : IClassFixture<ECommerceWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetOrders_ShouldReturnPagedResult()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        page.Should().NotBeNull();
        page!.Page.Should().BeGreaterThanOrEqualTo(1);
        page.PageSize.Should().BeGreaterThan(0);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "test@example.com",
            lines = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Laptop", unitPrice = 999.99, quantity = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturn400_WhenEmailInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "not-an-email",
            lines = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Laptop", unitPrice = 999.99, quantity = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturn400_WhenLinesEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "test@example.com",
            lines = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOrder()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "detail@test.com",
            lines = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Mouse", unitPrice = 50.0, quantity = 3 }
            }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/orders/{createBody!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order!.CustomerEmail.Should().Be("detail@test.com");
        order.Status.Should().Be("Pending");
        order.Lines.Should().ContainSingle().Which.ProductName.Should().Be("Mouse");
    }

    [Fact]
    public async Task GetOrderById_ShouldReturn404_WhenNotExists()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public record OrderResponse(Guid Id, string CustomerEmail, string Status, DateTime CreatedAt, List<OrderItemResponse> Lines);
public record OrderItemResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
