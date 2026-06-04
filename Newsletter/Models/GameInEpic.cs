using System.ComponentModel.DataAnnotations.Schema;

namespace Newsletter.Models
{
    public class GameInEpic
    {
        public Guid GameId { get; set; }

        public string EpicStoreId { get; set; } = null!;

        public decimal EpicPrice { get; set; }
        public decimal EpicFinalPrice { get; set; }
        public int EpicDiscountPercentage { get; set; }
        public bool EpicOnOffer { get; set; }
        public DateTime? LastPriceCheck { get; set; }
        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
    }
}
