using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        // Relations

        [ForeignKey(nameof(User1Id))]
        public virtual User User1 { get; set; }

        [ForeignKey(nameof(User2Id))]
        public virtual User User2 { get; set; }
    }
}
