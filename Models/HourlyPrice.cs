using System.ComponentModel.DataAnnotations;

namespace StrømAPI.Models;
public class HourlyPrice
{
    [Key]
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public double Price { get; set; }
    public string Area { get; set; }
    public bool Predicted { get; set; }

    public HourlyPrice(TimeOnly time,DateOnly date , double price, string area)
    {
        Date = date;
        Time = time;
        Price = price;
        Area = area;
        Predicted = false;
    }
    public HourlyPrice()
    {
    }
}

