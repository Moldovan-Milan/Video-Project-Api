using AutoMapper;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Data
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>().ForMember(dest => dest.FollowersCount, opt => opt.MapFrom(src => src.Followers.Count));

            CreateMap<Video, VideoDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments));
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
            CreateMap<User, UserWithVideosDto>()
                .ForMember(dest => dest.Videos, opt => opt.MapFrom(src =>
                src.Videos));
            CreateMap<UserChats, UserChatsDto>();

            CreateMap<LiveStream, LiveStreamDto>();
        }
    }
}
