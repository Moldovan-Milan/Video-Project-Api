using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OmegaStreamServices.Models
{
    [Table("users")]
    public class User: IdentityUser
    {
        [Column("avatar_id")]
        [Required] 
        public int AvatarId { get; set; } 
        [ForeignKey("AvatarId")] 
        public virtual Image? Avatar { get; set; } 
        [Column("followers")] [Required] 
        public int Followers { get; set; } 
        [Column("created")] 
        [Required] 
        public DateTime Created { get; set; }

        //[Column("verified")]
        //[Required]
        //public bool Verified { get; set; }

        // Relationships

        public virtual ICollection<VideoLikes> VideoLikes { get; set; }

        [JsonIgnore]
        public virtual ICollection<Video> Videos { get; set; }
    }
}
