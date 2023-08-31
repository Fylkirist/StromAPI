using System.ComponentModel.DataAnnotations;

namespace StromAPI.Models
{
    public class MagazineStock
    {
        [Key]
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public int Area { get; set; }
        public string AreaType { get; set; }
        public double Capacity { get; set; }
        public double Filling { get; set; }
        public double FillingFactor { get; set; }
        public double FillingFactorLastWeek { get; set; }
        public double FillingFactorChange { get; set; }

        public MagazineStock()
        {

        }
        public MagazineStock(DateOnly date, int area, string areaType, double capacity, double filling, double fillingFactor, double fillingFactorLastWeek, double fillingFactorChange)
        {
            Date = date;
            Area = area;
            AreaType = areaType;
            Capacity = capacity;
            Filling = filling;
            FillingFactor = fillingFactor;
            FillingFactorLastWeek = fillingFactorLastWeek;
            FillingFactorChange = fillingFactorChange;
        }
    }
}
