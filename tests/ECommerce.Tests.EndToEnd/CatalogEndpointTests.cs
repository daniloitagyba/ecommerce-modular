using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Tests.EndToEnd;

public class CatalogEndpointTests(ECommerceWebAppFactory factory) : IClassFixture<ECommerceWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetProducts_ShouldReturnEmptyPage_Initially()
    {
        var response = await _client.GetAsync("/api/catalog/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        page!.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/catalog/categories",
            new { name = "Electronics", description = "Devices" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnCreated()
    {
        // Create category first
        var catResponse = await _client.PostAsJsonAsync("/api/catalog/categories",
            new { name = "Books", description = "Reading material" });
        var catBody = await catResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.PostAsJsonAsync("/api/catalog/products",
            new { name = "C# in Depth", sku = "BK-001", price = 49.99, stockQuantity = 100, categoryId = catBody!.Id });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProductById_ShouldReturnProduct()
    {
        var catResponse = await _client.PostAsJsonAsync("/api/catalog/categories",
            new { name = "Gadgets", description = "Tech" });
        var catBody = await catResponse.Content.ReadFromJsonAsync<IdResponse>();

        var createResponse = await _client.PostAsJsonAsync("/api/catalog/products",
            new { name = "Phone", sku = "PH-001", price = 799.99, stockQuantity = 30, categoryId = catBody!.Id });
        var createBody = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/catalog/products/{createBody!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Name.Should().Be("Phone");
        product.Price.Should().Be(799.99m);
    }

    [Fact]
    public async Task GetProductById_ShouldReturn404_WhenNotExists()
    {
        var response = await _client.GetAsync($"/api/catalog/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStock_ShouldReturn204()
    {
        var catResponse = await _client.PostAsJsonAsync("/api/catalog/categories",
            new { name = "Tools", description = "Hardware" });
        var catBody = await catResponse.Content.ReadFromJsonAsync<IdResponse>();

        var createResponse = await _client.PostAsJsonAsync("/api/catalog/products",
            new { name = "Hammer", sku = "HM-001", price = 25.00, stockQuantity = 10, categoryId = catBody!.Id });
        var createBody = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.PutAsJsonAsync(
            $"/api/catalog/products/{createBody!.Id}/stock",
            new { quantity = 15 });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify stock was updated
        var getResponse = await _client.GetAsync($"/api/catalog/products/{createBody.Id}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        product!.StockQuantity.Should().Be(25); // 10 + 15
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400_WhenInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/catalog/products",
            new { name = "", sku = "", price = -1, stockQuantity = -1, categoryId = Guid.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400_WhenNameEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/catalog/categories",
            new { name = "", description = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

public record IdResponse(Guid Id);

public record ProductResponse(Guid Id, string Name, string Sku, decimal Price, int StockQuantity, string CategoryName);

public record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages, bool HasPrevious, bool HasNext);
