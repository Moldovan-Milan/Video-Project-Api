using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Dto
{
    public class VideoDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Path { get; set; }

        public string Extension { get; set; }

        public int ThumbnailId { get; set; }

        public virtual Image Thumbnail { get; set; }

        public TimeSpan Duration { get; set; }

        public string Description { get; set; }

        public string UserId { get; set; }

        public virtual UserDto User { get; set; }

        public string Status { get; set; }

        public DateTime Created { get; set; }

        public int Likes { get; set; }
        public int Dislikes { get; set; }
    }
}
