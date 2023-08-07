using Microsoft.EntityFrameworkCore;
namespace StrømAPI.Models;

public class HourlyPriceDB : DbContext
{
    public DbSet<HourlyPrice> Prices => Set<HourlyPrice>();
    public HourlyPriceDB(DbContextOptions<HourlyPriceDB> options):base(options)
    {

    }
}