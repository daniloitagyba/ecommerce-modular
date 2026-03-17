using System.Net;
using System.Net.Http.Json;
using ECommerce.API.BackgroundJobs;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ECommerce.Tests.EndToEnd;

public class BillingEndpointTests(ECommerceWebAppFactory factory) : IClassFixture<ECommerceWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> EnsureProductExistsAsync(string name = "Keyboard", decimal price = 150.0m, int stock = 100)
    {
        using var scope = factory.Services.CreateScope();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await catalogDb.Products.FirstOrDefaultAsync(p => p.Name == name);
        if (product is not null)
            return product.Id;

        var category = await catalogDb.Categories.FirstOrDefaultAsync()
            ?? ECommerce.Modules.Catalog.Domain.Category.Create("Billing Test", "Test").Value;
        if (!await catalogDb.Categories.AnyAsync())
        {
            catalogDb.Categories.Add(category);
            await catalogDb.SaveChangesAsync();
        }

        var newProduct = ECommerce.Modules.Catalog.Domain.Product.Create(
            name, $"BIL-{Guid.NewGuid():N}"[..20], price, stock, category.Id).Value;
        catalogDb.Products.Add(newProduct);
        await catalogDb.SaveChangesAsync();
        return newProduct.Id;
    }

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
    public async Task PlaceOrder_ShouldAsyncCreatePaymentAndInvoice()
    {
        var productId = await EnsureProductExistsAsync("Keyboard", 150.0m, 100);

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "billing@test.com",
            items = new[]
            {
                new { productId, quantity = 2 }
            }
        });
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderBody = await orderResponse.Content.ReadFromJsonAsync<IdResponse>();
        var orderId = orderBody!.Id;

        var payments = await PollForResultAsync<List<PaymentResponse>>(
            $"/api/billing/payments/{orderId}", r => r?.Count > 0);

        payments.Should().ContainSingle()
            .Which.Should().Match<PaymentResponse>(p =>
                p.OrderId == orderId &&
                p.Amount == 300.0m &&
                p.Status == "Completed");

        var invoices = await PollForResultAsync<List<InvoiceResponse>>(
            $"/api/billing/invoices/{orderId}", r => r?.Count > 0);

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
            items = new[]
            {
                new { productId = prodBody!.Id, quantity = 2 }
            }
        });
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderBody = await orderResponse.Content.ReadFromJsonAsync<IdResponse>();

        // 4. Verify order
        var getOrder = await _client.GetAsync($"/api/orders/{orderBody!.Id}");
        getOrder.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Wait for outbox processing, then verify payment
        var payments = await PollForResultAsync<List<PaymentResponse>>(
            $"/api/billing/payments/{orderBody.Id}", r => r?.Count > 0);
        payments.Should().ContainSingle().Which.Amount.Should().Be(1000m);

        // 6. Verify invoice
        var invoices = await PollForResultAsync<List<InvoiceResponse>>(
            $"/api/billing/invoices/{orderBody.Id}", r => r?.Count > 0);
        invoices.Should().ContainSingle().Which.Amount.Should().Be(1000m);
    }

    /// <summary>
    /// Polls an endpoint for a condition, manually triggering the outbox job
    /// on each iteration to avoid depending on Quartz scheduler timing
    /// (which can be unreliable under parallel test execution).
    /// </summary>
    private async Task<T> PollForResultAsync<T>(string url, Func<T?, bool> condition, int maxRetries = 15, int delayMs = 500)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            await TriggerOutboxProcessingAsync();

            var response = await _client.GetAsync(url);
            var result = await response.Content.ReadFromJsonAsync<T>();
            if (condition(result))
                return result!;
            await Task.Delay(delayMs);
        }

        var finalResponse = await _client.GetAsync(url);
        return (await finalResponse.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task TriggerOutboxProcessingAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProcessOutboxJob>>();
        var job = new ProcessOutboxJob(dbContext, bus, logger);
        var jobContext = new FakeJobExecutionContext();
        await job.Execute(jobContext);
    }
}

public record PaymentResponse(Guid Id, Guid OrderId, decimal Amount, string Status, DateTime CreatedAt, DateTime? CompletedAt);
public record InvoiceResponse(Guid Id, string InvoiceNumber, Guid OrderId, decimal Amount, string CustomerEmail, DateTime IssuedAt);

/// <summary>
/// Minimal IJobExecutionContext for manually triggering the outbox job in tests.
/// </summary>
internal sealed class FakeJobExecutionContext : IJobExecutionContext
{
    public IScheduler Scheduler => null!;
    public ITrigger Trigger => null!;
    public ICalendar? Calendar => null;
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => null!;
    public int RefireCount => 0;
    public JobDataMap MergedJobDataMap => new();
    public IJobDetail JobDetail => null!;
    public IJob JobInstance => null!;
    public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => null;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => null;
    public string FireInstanceId => "test";
    public object? Result { get; set; }
    public TimeSpan JobRunTime => TimeSpan.Zero;
    public CancellationToken CancellationToken => CancellationToken.None;
    public object? Get(object key) => null;
    public void Put(object key, object objectValue) { }
}
