using System.Text.Json;
using StrømAPI.Models;
using System.Text.Json.Serialization;

namespace StrømAPI.Tasks;

public class PriceUpdateTask
{
    private readonly HourlyPriceDB _db;
    private readonly HttpClient _httpClient;

    public PriceUpdateTask(HourlyPriceDB db)
    {
        _db = db;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task GetTomorrowsPricesFromHks()
    {
        var timestamp = DateTime.Today.AddDays(1);
        Console.WriteLine($"Fetching prices for {timestamp.Date}");
        var month = timestamp.Month.ToString().PadLeft(2, '0');
        var day = timestamp.Day.ToString().PadLeft(2, '0');
        string[] areas = { "NO1", "NO2", "NO3", "NO4", "NO5" };

        List<HourlyPrice> prices = new List<HourlyPrice>();

        foreach (var area in areas)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://www.hvakosterstrommen.no/api/v1/prices/{timestamp.Year}/{month}-{day}_{area}.json");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var deserialized = await response.Content.ReadFromJsonAsync<EnergyData[]>();

                    if (deserialized == null) continue;
                    foreach (var priceData in deserialized)
                    {
                        if (area != "NO4")
                        {
                            priceData.NOKPerKWh *= 1.25;
                            priceData.NOKPerKWh += 0.1541;
                        }
                        TimeOnly timeStamp = TimeOnly.FromDateTime(priceData.StartTime);
                        DateOnly dateStamp = DateOnly.FromDateTime(priceData.StartTime);
                        prices.Add(new HourlyPrice(timeStamp, dateStamp, priceData.NOKPerKWh, area));
                    }
                }
                else
                {
                    Console.WriteLine(response.StatusCode + "\n" + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fetch failed:");
                Console.WriteLine(ex.Message);
            }
        }
        await _db.Prices.AddRangeAsync(prices);
        await _db.SaveChangesAsync();
        Console.WriteLine($"{prices.Count} Prices added to database");
    }

    public async Task LoadHistoricalPricesFromHks(int daysBack)
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var time = DateTime.Today;

        List<HourlyPrice> prices = new List<HourlyPrice>();
        string[] areas = { "NO1", "NO2", "NO3", "NO4", "NO5" };
        Console.WriteLine($"Fetching historical prices for the last {daysBack} days...");
        for (int i = 0; i < daysBack; i++)
        {
            var timestamp = time.Subtract(TimeSpan.FromDays(i));
            var month = timestamp.Month.ToString().PadLeft(2, '0');
            var day = timestamp.Day.ToString().PadLeft(2, '0');

            foreach (var area in areas)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://www.hvakosterstrommen.no/api/v1/prices/{timestamp.Year}/{month}-{day}_{area}.json");
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var deserialized = await response.Content.ReadAsStringAsync();
                        EnergyData[] datas = JsonSerializer.Deserialize<EnergyData[]>(deserialized);
                        if (datas == null) continue;
                        foreach (var priceData in datas)
                        {
                            if (area != "NO4")
                            {
                                priceData.NOKPerKWh *= 1.25;
                                priceData.NOKPerKWh += 0.1541;
                            }
                            TimeOnly timeStamp = TimeOnly.FromDateTime(priceData.StartTime);
                            DateOnly dateStamp = DateOnly.FromDateTime(priceData.StartTime);
                            prices.Add(new HourlyPrice(timeStamp, dateStamp, priceData.NOKPerKWh>=0? priceData.NOKPerKWh:0, area));
                        }
                    }
                    else
                    {
                        Console.WriteLine(response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fetch failed:");
                    Console.WriteLine(ex.Message);
                }

            }
        }
        await _db.Prices.AddRangeAsync(prices);
        await _db.SaveChangesAsync();
        Console.WriteLine($"{prices.Count} Prices added to database");
    }
}


public class EnergyData
{
    [JsonPropertyName("NOK_per_kWh")]
    public double NOKPerKWh { get; set; }

    [JsonPropertyName("EUR_per_kWh")]
    public double EURPerKWh { get; set; }

    [JsonPropertyName("EXR")]
    public double ExchangeRate { get; set; }

    [JsonPropertyName("time_start")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("time_end")]
    public DateTime EndTime { get; set; }
}