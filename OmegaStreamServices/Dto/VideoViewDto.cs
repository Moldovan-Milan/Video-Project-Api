using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Dto
{
    public class VideoViewDto
    {
        public string UserId { get; set; }
        public UserDto User { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
        public int VideoId { get; set; }
        public VideoDto Video { get; set; }
    }
}
