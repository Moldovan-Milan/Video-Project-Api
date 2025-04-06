using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class UserVideoUpload
    {
        public string UserId { get; set; }
        public string VideoName { get; set; }
        public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.AddMinutes(5); // Default expiration date is 5 minutes from upload
    }
}
