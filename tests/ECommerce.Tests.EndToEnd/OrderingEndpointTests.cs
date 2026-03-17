using System.Net;
using System.Net.Http.Json;
using ECommerce.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.EndToEnd;

public class OrderingEndpointTests(ECommerceWebAppFactory factory) : IClassFixture<ECommerceWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> GetSeededProductIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await catalogDb.Products.FirstOrDefaultAsync();
        if (product is not null)
            return product.Id;

        // Seed a product if none exist
        var category = (await catalogDb.Categories.FirstOrDefaultAsync())
            ?? ECommerce.Modules.Catalog.Domain.Category.Create("Test Category", "Test").Value;
        if (!await catalogDb.Categories.AnyAsync())
        {
            catalogDb.Categories.Add(category);
            await catalogDb.SaveChangesAsync();
        }

        var newProduct = ECommerce.Modules.Catalog.Domain.Product.Create(
            "Test Product", "TEST-SKU-001", 99.99m, 100, category.Id).Value;
        catalogDb.Products.Add(newProduct);
        await catalogDb.SaveChangesAsync();
        return newProduct.Id;
    }

    [Fact]
    public async Task GetOrders_ShouldReturnPagedResult()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        page.Should().NotBeNull();
        page.Page.Should().BeGreaterThanOrEqualTo(1);
        page.PageSize.Should().BeGreaterThan(0);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnCreated()
    {
        var productId = await GetSeededProductIdAsync();

        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "test@example.com",
            items = new[]
            {
                new { productId, quantity = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturn400_WhenEmailInvalid()
    {
        var productId = await GetSeededProductIdAsync();

        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "not-an-email",
            items = new[]
            {
                new { productId, quantity = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturn400_WhenItemsEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "test@example.com",
            items = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOrder()
    {
        var productId = await GetSeededProductIdAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "detail@test.com",
            items = new[]
            {
                new { productId, quantity = 3 }
            }
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/orders/{createBody!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order!.CustomerEmail.Should().Be("detail@test.com");
        order.Status.Should().Be("Pending");
        order.TotalAmount.Should().BeGreaterThan(0);
        order.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetOrderById_ShouldReturn404_WhenNotExists()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public record OrderResponse(Guid Id, string CustomerEmail, string Status, DateTime CreatedAt, decimal TotalAmount, List<OrderItemResponse> Items);
public record OrderItemResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
