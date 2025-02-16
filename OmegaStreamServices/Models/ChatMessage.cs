using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int UserChatId { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}
