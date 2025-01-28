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
            CreateMap<User, UserDto>();
            CreateMap<Video, VideoDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
        }
    }
}
