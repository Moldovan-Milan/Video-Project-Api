using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public class CommentService: ICommentService
    {
        private readonly IGenericRepository _repo;
        private readonly UserManager<User> _userManager;

        public CommentService(UserManager<User> userManager, IGenericRepository repo)
        {
            _userManager = userManager;
            _repo = repo;
        }

        public async Task<int> AddNewComment(NewCommentDto newComment, string userId)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            Video? video = await _repo.FirstOrDefaultAsync<Video>(
                predicate: x => x.Id == newComment.VideoId);

            if (user == null || video == null)
            {
                return -1;
            }

            Comment comment = new Comment
            {
                Content = newComment.Content,
                UserId = user.Id,
                Created = DateTime.Now,
                VideoId = newComment.VideoId,
                User = user,
                Video = video
            };
            await _repo.AddAsync(comment);
            return comment.Id;
        }
    }
}