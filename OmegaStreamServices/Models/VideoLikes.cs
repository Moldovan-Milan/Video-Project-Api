using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class VideoLikes
    {
        public int VideoId { get; set; }
        public virtual Video Video { get; set; }

        public string UserId { get; set; }
        public virtual User User { get; set; }

        public bool IsDislike { get; set; }
    }
}
