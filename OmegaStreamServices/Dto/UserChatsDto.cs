using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Dto
{
    public class UserChatsDto
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public virtual UserDto User1 { get; set; }
        public virtual UserDto User2 { get; set; }
    }
}
