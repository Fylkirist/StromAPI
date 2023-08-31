using Microsoft.EntityFrameworkCore;

namespace StromAPI.Models;

public class MagazineStockDb : DbContext
{
    public DbSet<MagazineStock> MagazineStocks => Set<MagazineStock>();

    public MagazineStockDb(DbContextOptions<MagazineStockDb> options) : base(options)
    {

    }
}