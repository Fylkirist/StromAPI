using Microsoft.EntityFrameworkCore;
using StrømAPI.Models;
using StrømAPI.Tasks;

namespace StrømAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<HourlyPriceDB>(opt => opt.UseInMemoryDatabase("HourlyPrices"));

        builder.WebHost.UseWebRoot("wwwroot");

        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        app.UseStaticFiles();
        app.MapGet("/pris/{area}/{date}", async (HourlyPriceDB priceDb, string area, string date) =>
        {
            DateTime parsedDate = DateTime.Parse(date);
            DateOnly dateOnly = DateOnly.FromDateTime(parsedDate);

            var prices = await priceDb.Prices
                .Where(res => res.Area == area && res.Date == dateOnly)
                .ToListAsync();

            return Results.Ok(prices);
        });


        app.MapGet("/pris/gjennomsnitt/{area}",
            async (HourlyPriceDB priceDb, string area) =>
            {
                var priceList = await priceDb.Prices.Where(res => res.Area == area).ToArrayAsync();
                return priceList.Sum(e => e.Price) / priceList.Length;
            });

        app.MapFallback(async context =>
        {
            var webRootPath = app.Environment.WebRootPath;
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(Path.Combine(webRootPath, "index.html"));
        });

        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<HourlyPriceDB>();

        var updater = new PriceUpdateTask(db);
        await updater.LoadHistoricalPricesFromHks(5);

        var dbUpdateScheduler = new TaskSchedulerService(updater.GetTomorrowsPricesFromHks, new TimeSpan(15, 30, 0));

        await app.RunAsync();
    }
}