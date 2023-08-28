using Microsoft.EntityFrameworkCore;
using StrømAPI.Models;
using StrømAPI.Tasks;

namespace StrømAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<HourlyPriceDB>(opt => opt.UseSqlite("Data Source=PriceData.db"));

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
            if (date.Length != 10)
            {
                return Results.BadRequest();
            }
            DateTime parsedDate = DateTime.Parse(date);
            DateOnly dateOnly = DateOnly.FromDateTime(parsedDate);

            var prices = await priceDb.Prices
                .Where(res => res.Area == area && res.Date == dateOnly)
                .ToListAsync();
            if (prices.Count == 0)
            {
                Console.WriteLine($"No prices found for {dateOnly}: Predictor model used");
                prices.AddRange(predictor.PredictDate(dateOnly,area));
            }
            return Results.Ok(prices);
        });

        app.MapGet("/pris/{area}/{fromDate}/{toDate}",
            async (HourlyPriceDB priceDb, string area, string fromDate, string toDate) =>
            {
                if (fromDate.Length != 10 || toDate.Length != 10)
                {
                    return Results.BadRequest();
                }
                DateTime parsedFromDate = DateTime.Parse(fromDate);
                DateOnly fromDateOnly = DateOnly.FromDateTime(parsedFromDate);

                DateTime parsedToDate = DateTime.Parse(toDate);
                DateOnly toDateOnly = DateOnly.FromDateTime(parsedToDate);

                var prices = await priceDb.Prices
                    .Where(res => res.Area == area && res.Date >= fromDateOnly && res.Date <= toDateOnly)
                    .ToListAsync();

                var dateIterator = fromDateOnly;
                while (dateIterator < toDateOnly)
                {
                    var iterator = dateIterator;
                    if (prices.All(res => res.Date != iterator))
                    {
                        Console.WriteLine($"No prices found for {iterator}: Predictor model used");
                        prices.AddRange(predictor.PredictDate(dateIterator,area));
                    }
                    dateIterator = dateIterator.AddDays(1);
                }

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
        await updater.LoadHistoricalPricesFromHks(720);
        if(DateTime.Now > new DateTime(DateTime.Today.Year,DateTime.Today.Month,DateTime.Today.Day,15,30,0))
            await updater.GetTomorrowsPricesFromHks();

        var dbUpdateScheduler = new TaskSchedulerService(updater.GetTomorrowsPricesFromHks, new TimeSpan(15, 30, 0));
        dbUpdateScheduler.ScheduleNextExecution();

        predictor.Initialize();
        await app.RunAsync();
    }
}