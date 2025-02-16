using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class UserChats
    {
        public int Id { get; set; }
        public string User1Id { get; set; }
        public string User2Id { get; set; }
        public DateTime Created { get; set; }
    }
}
