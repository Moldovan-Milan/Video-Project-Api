using OmegaStreamServices.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface ICommentService
    {
        Task<int> AddNewComment(NewCommentDto newComment, string userId);

    }
}
