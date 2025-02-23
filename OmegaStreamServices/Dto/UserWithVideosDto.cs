using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Dto
{
    public class UserWithVideosDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int AvatarId { get; set; }
        public int FollowersCount { get; set; }
        public ICollection<VideoDto> Videos { get; set; }
    }
}
