using AutoMapper;
using VideoProjektAspApi.Model;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
