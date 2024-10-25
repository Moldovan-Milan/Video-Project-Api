using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoProjektAspApi.Model
{
    [Table("videos")]
    public class Video
    {
        [Key]
        public int Id { get; set; }

        [Column("path")]
        [Required]
        public string Path { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("extension")]
        [Required]
        public string Extension { get; set; }

        [Column("created")]
        [Required]
        public DateTime Created { get; set; }

        [Column("duration")]
        public TimeSpan Duration { get; set; }

        [Column("thumbnail_path")]
        public string ThumbnailPath { get; set; }
    }
}
