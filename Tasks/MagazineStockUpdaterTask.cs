using StromAPI.Models;
using System.Text.Json;
using System.Xml;

namespace StromAPI.Tasks;

public class MagazineStockUpdaterTask
{
    private HttpClient _httpClient;
    private MagazineStockDb _db;

    public MagazineStockUpdaterTask(MagazineStockDb db)
    {
        _httpClient = new HttpClient();
        _db = db;
    }

    public async Task UpdateHistoricalData()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://biapi.nve.no/magasinstatistikk/api/Magasinstatistikk/HentOffentligData");
        
        var response = await _httpClient.SendAsync(request);

        var deserialized = await response.Content.ReadFromJsonAsync<MagazineStockData[]>();
        if (deserialized == null)
        {
            Console.WriteLine("Magazine data fetch failed!");
            return;
        }

        var counter = 0;
        foreach (var data in deserialized)
        {
            if (!_db.MagazineStocks.Any(e => 
                    e.Date == DateOnly.Parse(data.dato_Id) && 
                    data.omrnr == e.Area && 
                    data.omrType == e.AreaType))
            {
                counter++;
                MagazineStock newData = new MagazineStock(DateOnly.Parse(data.dato_Id), data.omrnr, data.omrType, data.kapasitet_TWh, data.fylling_TWh, data.fyllingsgrad, data.fyllingsgrad_forrige_uke, data.endring_fyllingsgrad);
                _db.Add(newData);
            }
        }

        Console.WriteLine($"{counter} Magazine stock data points added!");
        await _db.SaveChangesAsync();
    }

    public async Task UpdateWeeklyData()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://biapi.nve.no/magasinstatistikk/api/Magasinstatistikk/HentOffentligDataSisteUke");

        var response = await _httpClient.SendAsync(request);

        var deserialized = await response.Content.ReadFromJsonAsync<MagazineStockData[]>();
        if (deserialized == null)
        {
            Console.WriteLine("Magazine data fetch failed!");
            return;
        }

        var counter = 0;
        foreach (var data in deserialized)
        {
            if (!_db.MagazineStocks.Any(e =>
                    e.Date == DateOnly.Parse(data.dato_Id) &&
                    data.omrnr == e.Area &&
                    data.omrType == e.AreaType))
            {
                counter++;
                MagazineStock newData = new MagazineStock(DateOnly.Parse(data.dato_Id),data.omrnr,data.omrType,data.kapasitet_TWh,data.fylling_TWh,data.fyllingsgrad,data.fyllingsgrad_forrige_uke,data.endring_fyllingsgrad);
                _db.Add(newData);
            }
        }

        Console.WriteLine($"{counter} Magazine stock data points added!");
        await _db.SaveChangesAsync();
    }
}

public class MagazineStockData
{
    public string dato_Id { get; set; }
    public string omrType { get; set; }
    public int omrnr { get; set; }
    public int iso_aar { get; set; }
    public int iso_uke { get; set; }
    public double fyllingsgrad { get; set; }
    public double kapasitet_TWh { get; set; }
    public double fylling_TWh { get; set; }
    public DateTime neste_Publiseringsdato { get; set; }
    public double fyllingsgrad_forrige_uke { get; set; }
    public double endring_fyllingsgrad { get; set; }

    public MagazineStockData()
    {

    }
}