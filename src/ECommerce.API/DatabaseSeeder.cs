using System.Security.Cryptography;
using System.Text;
using ECommerce.Modules.Billing.Domain;
using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Domain;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Domain;
using ECommerce.Modules.Ordering.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API;

public static class DatabaseSeeder
{
    private static readonly Guid SeedNamespace = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private static Guid DeterministicGuid(string name)
    {
        var input = $"{SeedNamespace}:{name}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static void SetEntityId(object entity, Guid id)
    {
        var prop = typeof(ECommerce.Shared.Domain.Entity).GetProperty("Id")!;
        prop.SetValue(entity, id);
    }

    public static async Task SeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var orderingDb = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var billingDb = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        if (await catalogDb.Categories.AnyAsync())
            return;

        var random = new Random(42);

        // --- Categories ---
        var categoryData = new[]
        {
            ("Electronics", "Smartphones, laptops, tablets and accessories"),
            ("Clothing", "Men's and women's apparel and fashion accessories"),
            ("Home & Kitchen", "Furniture, appliances and home decor"),
            ("Books", "Fiction, non-fiction, technical and educational books"),
            ("Sports & Outdoors", "Equipment, apparel and accessories for sports"),
            ("Beauty & Personal Care", "Skincare, makeup and grooming products"),
            ("Toys & Games", "Board games, puzzles, action figures and more"),
            ("Automotive", "Car parts, accessories and maintenance products"),
        };

        var categories = new List<Category>();
        foreach (var (name, desc) in categoryData)
        {
            var cat = Category.Create(name, desc).Value!;
            SetEntityId(cat, DeterministicGuid($"category:{name}"));
            categories.Add(cat);
        }

        catalogDb.Categories.AddRange(categories);
        await catalogDb.SaveChangesAsync();

        // --- Products (15-20 per category) ---
        var productTemplates = new Dictionary<string, (string Name, string SkuPrefix, decimal MinPrice, decimal MaxPrice)[]>
        {
            ["Electronics"] =
            [
                ("iPhone 15 Pro", "ELEC-IPH15", 999.99m, 1399.99m),
                ("Samsung Galaxy S24", "ELEC-SGS24", 799.99m, 1199.99m),
                ("MacBook Air M3", "ELEC-MBA-M3", 1099.00m, 1499.00m),
                ("Dell XPS 15", "ELEC-DXPS15", 1299.00m, 1799.00m),
                ("iPad Pro 12.9", "ELEC-IPDP12", 1099.00m, 1599.00m),
                ("Sony WH-1000XM5", "ELEC-SNWHX5", 349.99m, 399.99m),
                ("AirPods Pro 2", "ELEC-APDP2", 229.00m, 249.00m),
                ("Nintendo Switch OLED", "ELEC-NSWOL", 349.99m, 349.99m),
                ("Kindle Paperwhite", "ELEC-KNDPW", 139.99m, 189.99m),
                ("LG OLED C3 55\"", "ELEC-LGC355", 1299.99m, 1799.99m),
                ("Bose QC Ultra", "ELEC-BSQCU", 329.00m, 429.00m),
                ("Logitech MX Master 3S", "ELEC-LGMXM3", 99.99m, 99.99m),
                ("Samsung T7 SSD 1TB", "ELEC-SMT71T", 89.99m, 109.99m),
                ("Apple Watch Series 9", "ELEC-AWS9", 399.00m, 799.00m),
                ("Google Pixel 8", "ELEC-GPX8", 699.00m, 999.00m),
                ("Anker PowerBank 20K", "ELEC-ANKPB", 49.99m, 69.99m),
            ],
            ["Clothing"] =
            [
                ("Levi's 501 Original Jeans", "CLTH-LV501", 59.99m, 89.99m),
                ("Nike Air Max 90", "CLTH-NKAM90", 119.99m, 139.99m),
                ("Adidas Ultraboost 22", "CLTH-ADUB22", 149.99m, 189.99m),
                ("Patagonia Down Jacket", "CLTH-PTGDJ", 229.00m, 329.00m),
                ("Ralph Lauren Polo Shirt", "CLTH-RLPOLO", 89.99m, 125.00m),
                ("Uniqlo Heattech Thermal", "CLTH-UQHT", 19.99m, 29.99m),
                ("Zara Slim Fit Blazer", "CLTH-ZRBLZR", 89.99m, 149.99m),
                ("North Face Fleece", "CLTH-NFFLC", 99.00m, 149.00m),
                ("Calvin Klein T-Shirt Pack", "CLTH-CKTP3", 39.99m, 49.99m),
                ("Converse Chuck Taylor", "CLTH-CNVCT", 55.00m, 75.00m),
                ("Tommy Hilfiger Hoodie", "CLTH-THHD", 79.99m, 119.99m),
                ("H&M Linen Shirt", "CLTH-HMLNS", 29.99m, 39.99m),
                ("Under Armour Shorts", "CLTH-UASHRT", 34.99m, 44.99m),
                ("New Balance 574", "CLTH-NB574", 79.99m, 99.99m),
                ("Champion Reverse Weave", "CLTH-CHPRW", 54.99m, 69.99m),
            ],
            ["Home & Kitchen"] =
            [
                ("Instant Pot Duo 7-in-1", "HOME-IPDT7", 89.99m, 99.99m),
                ("Dyson V15 Detect", "HOME-DYNV15", 649.99m, 749.99m),
                ("KitchenAid Stand Mixer", "HOME-KASM", 329.99m, 449.99m),
                ("Nespresso Vertuo Next", "HOME-NSPVN", 159.00m, 199.00m),
                ("Philips Air Fryer XXL", "HOME-PHAFX", 199.99m, 249.99m),
                ("iRobot Roomba j7+", "HOME-IRBJ7", 599.99m, 799.99m),
                ("Le Creuset Dutch Oven", "HOME-LCDOV", 299.95m, 379.95m),
                ("Vitamix Blender A3500", "HOME-VTMXA", 549.95m, 649.95m),
                ("Casper Original Mattress", "HOME-CSPMQ", 995.00m, 1295.00m),
                ("Sonos One Speaker", "HOME-SNSO1", 199.00m, 219.00m),
                ("Breville Barista Express", "HOME-BRVBE", 699.95m, 699.95m),
                ("Weber Spirit Grill", "HOME-WBSPR", 449.00m, 549.00m),
                ("Calphalon Pan Set 10pc", "HOME-CLPH10", 249.99m, 299.99m),
                ("Shark Navigator Vacuum", "HOME-SHKNV", 159.99m, 199.99m),
                ("Keurig K-Elite", "HOME-KRGE", 149.99m, 169.99m),
                ("Cuisinart Food Processor", "HOME-CSNFP", 179.99m, 199.99m),
            ],
            ["Books"] =
            [
                ("Clean Code", "BOOK-CLNCD", 34.99m, 44.99m),
                ("Designing Data-Intensive Apps", "BOOK-DDIA", 39.99m, 49.99m),
                ("The Pragmatic Programmer", "BOOK-TPRGP", 44.99m, 54.99m),
                ("Domain-Driven Design", "BOOK-DDD", 49.99m, 64.99m),
                ("System Design Interview", "BOOK-SDINT", 29.99m, 39.99m),
                ("Atomic Habits", "BOOK-ATMHB", 16.99m, 24.99m),
                ("Sapiens", "BOOK-SAPNS", 18.99m, 24.99m),
                ("Thinking Fast and Slow", "BOOK-THKFS", 14.99m, 19.99m),
                ("The Lean Startup", "BOOK-TLNST", 19.99m, 26.99m),
                ("Refactoring", "BOOK-RFCTR", 44.99m, 54.99m),
                ("Patterns of Enterprise Apps", "BOOK-PEAA", 54.99m, 64.99m),
                ("Head First Design Patterns", "BOOK-HFDP", 39.99m, 49.99m),
                ("Cracking the Coding Interview", "BOOK-CTCI", 29.99m, 39.99m),
                ("Building Microservices", "BOOK-BLDMS", 39.99m, 49.99m),
                ("You Don't Know JS", "BOOK-YDKJS", 24.99m, 34.99m),
                ("Eloquent JavaScript", "BOOK-ELQJS", 29.99m, 39.99m),
                ("Introduction to Algorithms", "BOOK-ITOA", 79.99m, 99.99m),
            ],
            ["Sports & Outdoors"] =
            [
                ("Yeti Rambler 30oz", "SPRT-YTRM30", 35.00m, 38.00m),
                ("Fitbit Charge 5", "SPRT-FTBC5", 129.95m, 149.95m),
                ("Hydro Flask 32oz", "SPRT-HYFL32", 39.95m, 44.95m),
                ("Coleman Sundome Tent 4P", "SPRT-CLMN4", 79.99m, 109.99m),
                ("Garmin Forerunner 265", "SPRT-GRMFR", 349.99m, 449.99m),
                ("Osprey Atmos 65L", "SPRT-OSPA65", 259.95m, 289.95m),
                ("REI Half Dome Tent", "SPRT-REIHD", 229.00m, 279.00m),
                ("Manduka Pro Yoga Mat", "SPRT-MNDYK", 99.00m, 120.00m),
                ("Black Diamond Headlamp", "SPRT-BDHLD", 29.99m, 39.99m),
                ("TRX Suspension Trainer", "SPRT-TRXST", 149.95m, 199.95m),
                ("Bowflex Adjustable Dumbbells", "SPRT-BWFDB", 329.00m, 429.00m),
                ("NordicTrack Treadmill", "SPRT-NRDTM", 999.00m, 1499.00m),
                ("Patagonia Black Hole Bag", "SPRT-PTGBH", 89.00m, 129.00m),
                ("The North Face Backpack", "SPRT-TNFBK", 69.00m, 99.00m),
                ("Therm-a-Rest Sleeping Pad", "SPRT-THRSP", 149.95m, 199.95m),
            ],
            ["Beauty & Personal Care"] =
            [
                ("CeraVe Moisturizing Cream", "BEAU-CRVMC", 16.99m, 19.99m),
                ("The Ordinary Niacinamide", "BEAU-TONIA", 6.50m, 8.90m),
                ("Dyson Airwrap", "BEAU-DYNAW", 499.99m, 599.99m),
                ("Olaplex No.3 Treatment", "BEAU-OLPX3", 28.00m, 30.00m),
                ("La Roche-Posay Sunscreen", "BEAU-LRPSS", 29.99m, 36.99m),
                ("Drunk Elephant Protini", "BEAU-DEPRT", 68.00m, 68.00m),
                ("Paula's Choice BHA Exfoliant", "BEAU-PCBHA", 32.00m, 35.00m),
                ("Tatcha Dewy Skin Cream", "BEAU-TTCDS", 68.00m, 68.00m),
                ("Oral-B iO Series 9", "BEAU-OBIO9", 249.99m, 299.99m),
                ("Philips Sonicare 9900", "BEAU-PHSN9", 249.99m, 329.99m),
                ("SK-II Facial Treatment Essence", "BEAU-SKIIF", 185.00m, 235.00m),
                ("Neutrogena Retinol Cream", "BEAU-NTGRC", 22.99m, 28.99m),
                ("Moroccanoil Treatment", "BEAU-MRCOT", 34.00m, 48.00m),
                ("Kiehl's Ultra Facial Cream", "BEAU-KLUFC", 32.00m, 38.00m),
                ("Glossier Boy Brow", "BEAU-GLSBB", 17.00m, 17.00m),
                ("Summer Fridays Jet Lag Mask", "BEAU-SFJLM", 48.00m, 48.00m),
            ],
            ["Toys & Games"] =
            [
                ("LEGO Star Wars Millennium Falcon", "TOYS-LGSWM", 159.99m, 169.99m),
                ("Monopoly Classic", "TOYS-MNPLY", 19.99m, 24.99m),
                ("Settlers of Catan", "TOYS-CATAN", 39.99m, 44.99m),
                ("LEGO Technic Bugatti", "TOYS-LGTBG", 349.99m, 449.99m),
                ("Risk Board Game", "TOYS-RISKB", 29.99m, 34.99m),
                ("Ticket to Ride", "TOYS-TKTRD", 39.99m, 49.99m),
                ("Nerf Elite 2.0", "TOYS-NRFE2", 24.99m, 34.99m),
                ("Hot Wheels Track Set", "TOYS-HWTKS", 39.99m, 59.99m),
                ("Barbie Dreamhouse", "TOYS-BRBDH", 179.99m, 199.99m),
                ("Play-Doh Mega Pack", "TOYS-PLDMP", 14.99m, 19.99m),
                ("Rubik's Cube 3x3", "TOYS-RBKC3", 9.99m, 12.99m),
                ("Codenames Board Game", "TOYS-CDNMS", 14.99m, 19.99m),
                ("Pandemic Board Game", "TOYS-PNDMC", 34.99m, 44.99m),
                ("UNO Card Game", "TOYS-UNOCG", 5.99m, 9.99m),
                ("Jenga Classic", "TOYS-JNGCL", 12.99m, 16.99m),
            ],
            ["Automotive"] =
            [
                ("Armor All Car Wash Kit", "AUTO-AACWK", 24.99m, 34.99m),
                ("Michelin Wiper Blades", "AUTO-MCHWB", 19.99m, 29.99m),
                ("Chemical Guys Detailing Kit", "AUTO-CGDTK", 49.99m, 79.99m),
                ("Anker Roav Dash Cam", "AUTO-ANKDC", 55.99m, 79.99m),
                ("NOCO Boost Jump Starter", "AUTO-NCBJS", 99.95m, 149.95m),
                ("Mobil 1 Synthetic Oil 5qt", "AUTO-MB1SO", 27.97m, 34.97m),
                ("Rain-X Glass Treatment", "AUTO-RNXGT", 5.99m, 8.99m),
                ("Meguiar's Gold Class Wax", "AUTO-MGRGC", 19.99m, 24.99m),
                ("WeatherTech Floor Mats", "AUTO-WTFMT", 89.95m, 149.95m),
                ("Thinkware Dash Cam F200", "AUTO-TKWF2", 129.99m, 179.99m),
                ("Tire Pressure Gauge Digital", "AUTO-TPGDG", 12.99m, 18.99m),
                ("LED Headlight Bulbs H11", "AUTO-LEDH11", 29.99m, 49.99m),
                ("Car Phone Mount Magnetic", "AUTO-CPMMT", 14.99m, 24.99m),
                ("Portable Air Compressor", "AUTO-PACMP", 39.99m, 59.99m),
                ("Microfiber Towels 24-Pack", "AUTO-MFTW24", 13.99m, 19.99m),
            ],
        };

        var products = new List<Product>();
        foreach (var category in categories)
        {
            if (!productTemplates.TryGetValue(category.Name, out var templates))
                continue;

            foreach (var (name, skuPrefix, minPrice, maxPrice) in templates)
            {
                var price = Math.Round(minPrice + (maxPrice - minPrice) * (decimal)random.NextDouble(), 2);
                var stock = random.Next(5, 200);
                var sku = $"{skuPrefix}-{random.Next(100, 999)}";
                var product = Product.Create(name, sku, price, stock, category.Id).Value!;
                SetEntityId(product, DeterministicGuid($"product:{skuPrefix}"));
                products.Add(product);
            }
        }

        catalogDb.Products.AddRange(products);
        await catalogDb.SaveChangesAsync();

        // --- Orders, Payments & Invoices ---
        var emails = new[]
        {
            "alice.johnson@email.com", "bob.smith@email.com", "carol.williams@email.com",
            "david.brown@email.com", "emma.davis@email.com", "frank.miller@email.com",
            "grace.wilson@email.com", "henry.moore@email.com", "iris.taylor@email.com",
            "jack.anderson@email.com", "karen.thomas@email.com", "leo.jackson@email.com",
            "mia.white@email.com", "noah.harris@email.com", "olivia.martin@email.com",
            "peter.garcia@email.com", "quinn.martinez@email.com", "rachel.robinson@email.com",
            "sam.clark@email.com", "tina.rodriguez@email.com",
        };

        var statuses = new[] { OrderStatus.Confirmed, OrderStatus.Paid, OrderStatus.Shipped };

        for (var i = 0; i < 50; i++)
        {
            var email = emails[random.Next(emails.Length)];
            var itemCount = random.Next(1, 5);
            var selectedProducts = products.OrderBy(_ => random.Next()).Take(itemCount).ToList();

            var orderItems = selectedProducts.Select(p =>
                OrderItem.Create(p.Id, p.Name, p.Price, random.Next(1, 4))
            ).ToList();

            var orderResult = Order.Create(email, orderItems);
            if (orderResult.IsFailure) continue;

            var order = orderResult.Value!;
            var status = statuses[random.Next(statuses.Length)];
            order.Confirm();
            if (status >= OrderStatus.Paid) order.MarkAsPaid();
            if (status >= OrderStatus.Shipped) order.MarkAsShipped();

            orderingDb.Orders.Add(order);
            await orderingDb.SaveChangesAsync();

            // Create payment
            var payment = Payment.Create(order.Id, order.TotalAmount);
            if (status >= OrderStatus.Paid)
                payment.MarkAsCompleted();
            billingDb.Payments.Add(payment);
            await billingDb.SaveChangesAsync();

            // Create invoice for completed payments
            if (payment.Status == PaymentStatus.Completed)
            {
                var invoice = Invoice.Create(order.Id, payment.Id, email, order.TotalAmount);
                billingDb.Invoices.Add(invoice);
                await billingDb.SaveChangesAsync();
            }
        }

        var pendingMessages = await orderingDb.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .ToListAsync();

        foreach (var message in pendingMessages)
            message.ProcessedAt = DateTime.UtcNow;

        await orderingDb.SaveChangesAsync();
    }
}
