using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {// CreateMap<from where to go, to where to go>();
        CreateMap<AppUser, MemberDto>() //from AppUser to MemberDto
            .ForMember(d => d.PhotoUrl, o => 
                o.MapFrom(s => s.Photos.FirstOrDefault(x => x.IsMain)!.Url)); //! - null forgiving operator
        CreateMap<Photo, PhotoDto>();
    }
}
