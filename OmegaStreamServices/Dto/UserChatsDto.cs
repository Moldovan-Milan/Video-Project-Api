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
        public UserDto User { get; set; }
        public string LastMessage { get; set; } = string.Empty;
    }
}
