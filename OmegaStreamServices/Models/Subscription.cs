using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OmegaStreamServices.Models
{
    [Table("subscriptions")]
    public class Subscription
    {
        public int Id { get; set; }

        public string FollowerId { get; set; }
        public string FollowedUserId { get; set; }
        public DateTime FollowedAt { get; set; }

        // Relations
        [JsonIgnore]
        [ForeignKey(nameof(FollowerId))]
        public virtual User Follower { get; set; }

        [ForeignKey(nameof(FollowedUserId))]
        public virtual User FollowedUser { get; set; }
    }
}
