using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class LiveStream
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string StreamerConnectionId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string StreamTitle { get; set; }
        public string Description { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

}
