using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {//CreateMap<from where to go, to where to go>();
        CreateMap<AppUser, MemberDto>(); //from AppUser to MemberDto
        CreateMap<Photo, PhotoDto>();
    }
}
