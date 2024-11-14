using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoProjektAspApi.Model
{
    [Table("images")]
    public class Image
    {
        [Required]
        [Key]
        public int Id { get; set; }

        [Column("path")]
        [Required]
        public string Path { get; set; }

        [Column("extension")]
        [Required]
        public string Extension { get; set; }
    }
}
