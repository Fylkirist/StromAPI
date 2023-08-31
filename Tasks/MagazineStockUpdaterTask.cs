using StromAPI.Models;

namespace StromAPI.Tasks;

public class MagazineStockUpdaterTask
{
    private HttpClient _httpClient;
    private MagazineStockDb _db;

    public MagazineStockUpdaterTask(MagazineStockDb db)
    {
        _httpClient = new HttpClient();
        _db = db;
        _httpClient.Timeout = new TimeSpan(10);
    }

    public void UpdateHistoricalData()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://biapi.nve.no/magasinstatistikk/api/Magasinstatistikk/HentOffentligData");
        
        var response = _httpClient.SendAsync(request);


    }

    public void UpdateDailyData()
    {

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