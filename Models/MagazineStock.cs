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
        public float Capacity { get; set; }
        public float Filling { get; set; }
        public float FillingFactor { get; set; }
        public float FillingFactorLastWeek { get; set; }
        public float FillingFactorChange { get; set; }

        public MagazineStock()
        {

        }
    }
}
