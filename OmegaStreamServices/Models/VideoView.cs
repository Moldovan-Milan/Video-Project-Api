using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class VideoView
    {
        
        public string? UserId { get; set; }
        public virtual User? User { get; set; }

        [NotMapped]
        public string? IpAddressHash { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public int VideoId { get; set; }
        public virtual Video Video { get; set; }
    }
}
