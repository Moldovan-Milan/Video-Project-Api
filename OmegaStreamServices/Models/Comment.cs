using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    [Table("comments")]
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Created { get; set; }


        // Relations
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = new User();
       
    }
}
