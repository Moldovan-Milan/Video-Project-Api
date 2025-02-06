using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class Room
    {
        public string RoomId { get; set; }
        public List<WebSocket> Connections { get; set; } = new List<WebSocket>();
    }
}
