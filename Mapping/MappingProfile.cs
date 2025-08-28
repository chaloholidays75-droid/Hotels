using AutoMapper;
using HotelAPI.Models;
using HotelAPI.Models.DTO;

namespace HotelAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<HotelInfo, HotelDto>()
                .ForMember(dest => dest.Staff, opt => opt.MapFrom(src => src.HotelStaff));

            CreateMap<HotelStaff, HotelStaffDto>();
            
            CreateMap<HotelDto, HotelInfo>();
            CreateMap<HotelStaffDto, HotelStaff>();
        }
    }
}
