using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class VideoState
    {
        public double CurrentTime { get; set; }
        public bool IsPlaying { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
