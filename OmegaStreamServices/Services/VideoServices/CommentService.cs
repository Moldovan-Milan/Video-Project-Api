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
        private readonly ICommentRepositroy _commentRepositroy;
        private readonly IVideoRepository _videoRepository;
        private readonly UserManager<User> _userManager;

        public CommentService(ICommentRepositroy commentRepositroy, IVideoRepository videoRepository, UserManager<User> userManager)
        {
            _commentRepositroy = commentRepositroy;
            _videoRepository = videoRepository;
            _userManager = userManager;
        }

        public async Task<int> AddNewComment(NewCommentDto newComment, string userId)
        {
            User user = await _userManager.FindByIdAsync(userId);
            Video video = await _videoRepository.FindByIdAsync(newComment.VideoId);

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

            await _commentRepositroy.Add(comment);
            return comment.Id;
        }
    }
}