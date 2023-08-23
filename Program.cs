using Microsoft.EntityFrameworkCore;
using StrømAPI.Models;
using StrømAPI.Tasks;
using Microsoft.ML;

namespace StrømAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<HourlyPriceDB>(opt => opt.UseInMemoryDatabase("HourlyPrices"));

        builder.WebHost.UseWebRoot("wwwroot");

        builder.Services.AddCors();

        var app = builder.Build();

        app.UseCors(options =>options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<HourlyPriceDB>();
        var predictor = new PricePredictorService(db);

        app.UseStaticFiles();
        app.MapGet("/pris/{area}/{date}", async (HourlyPriceDB priceDb, string area, string date) =>
        {
            DateTime parsedDate = DateTime.Parse(date);
            DateOnly dateOnly = DateOnly.FromDateTime(parsedDate);

            var prices = await priceDb.Prices
                .Where(res => res.Area == area && res.Date == dateOnly)
                .ToListAsync();
            if (prices.Count == 0)
            {
                prices.AddRange(predictor.PredictDate(dateOnly,area));
            }
            return Results.Ok(prices);
        });

        app.MapGet("/pris/{area}/{fromDate}/{toDate}",
            async (HourlyPriceDB priceDb, string area, string fromDate, string toDate) =>
            {
                DateTime parsedFromDate = DateTime.Parse(fromDate);
                DateOnly fromDateOnly = DateOnly.FromDateTime(parsedFromDate);

                DateTime parsedToDate = DateTime.Parse(toDate);
                DateOnly toDateOnly = DateOnly.FromDateTime(parsedToDate);

                var prices = await priceDb.Prices
                    .Where(res => res.Area == area && res.Date >= fromDateOnly && res.Date <= toDateOnly)
                    .ToListAsync();


                return Results.Ok(prices);
            });


        app.MapGet("/pris/gjennomsnitt/{area}",
            async (HourlyPriceDB priceDb, string area) =>
            {
                var priceList = await priceDb.Prices.Where(res => res.Area == area).ToArrayAsync();
                return Results.Ok(priceList.Sum(e => e.Price) / priceList.Length);
            });

        app.MapFallback(async context =>
        {
            var webRootPath = app.Environment.WebRootPath;
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(Path.Combine(webRootPath, "index.html"));
        });

        

        var updater = new PriceUpdateTask(db);
        await updater.LoadHistoricalPricesFromHks(14);

        var dbUpdateScheduler = new TaskSchedulerService(updater.GetTomorrowsPricesFromHks, new TimeSpan(15, 30, 0));

        predictor.Initialize();
        await app.RunAsync();
    }
}