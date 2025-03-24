using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class UserTheme
    {
        [Required]
        public int Id { get; set; }
        public string? Background { get; set; }
        public string? TextColor { get; set; }
        public int? BannerId {  get; set; }
        [ForeignKey(nameof(BannerId))]
        public virtual Image? BannerImg { get; set; }


    }
}
