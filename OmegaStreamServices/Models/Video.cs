using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmegaStreamServices.Models
{
    [Table("videos")]
    public class Video
    {
        [Key]
        public int Id { get; set; }

        [Column("title")]
        [Required]
        public string Title { get; set; }

        [Column("path")]
        [Required]
        public string Path { get; set; }

        [Column("extension")]
        [Required]
        public string Extension { get; set; }

        [Column("thumbnail_id")]
        [Required]
        public int ThumbnailId { get; set; }

        [ForeignKey("ThumbnailId")]
        public virtual Image Thumbnail { get; set; }

        [Column("duration")]
        public TimeSpan Duration { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created")]
        [Required]
        public DateTime Created { get; set; }
        
        // Relationships

        public virtual ICollection<VideoLikes> VideoLikes { get; set; }
    }
}
