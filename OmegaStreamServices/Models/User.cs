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

        [Column("created")] 
        [Required] 
        public DateTime Created { get; set; }

        [Column("verification_requested")]
        [Required]
        public bool IsVerificationRequested { get; set; } = false;

        //[Column("verified")]
        //[Required]
        //public bool Verified { get; set; }

        // Relationships

        public virtual ICollection<VideoLikes> VideoLikes { get; set; }
        public virtual ICollection<VideoView> ViewHistory { get; set; }

        public virtual ICollection<Subscription> Following { get; set; } = new List<Subscription>();
        public virtual ICollection<Subscription> Followers { get; set; } = new List<Subscription>();

        [JsonIgnore]
        public virtual ICollection<Video> Videos { get; set; }
    }
}
