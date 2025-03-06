using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class RoomState
    {
        public User Host { get; set; } = new();
        public string HostConnId { get; set; } = string.Empty;
        public bool IsHostInRoom { get; set; } = false;
        public List<User> Members { get; set; } = new();
        public Dictionary<string, string> UserIdAndConnId { get; set; } = new();
        public Dictionary<string, string> WaitingForAccept { get; set; } = new();
        public VideoState VideoState { get; set; } = new VideoState();
    }
}
