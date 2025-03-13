using OmegaStreamServices.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class RoomMessage
    {
        public UserDto Sender { get; set; } = new();
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string Content { get; set; } = string.Empty;
    }
}
