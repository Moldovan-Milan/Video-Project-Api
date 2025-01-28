using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Dto
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public int AvatarId { get; set; }
        public int Followers { get; set; }
    }
}
