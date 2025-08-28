using AutoMapper;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
using System.Linq;

namespace HotelAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // HotelInfo -> HotelDto
            CreateMap<HotelInfo, HotelDto>()
                .ForMember(dest => dest.SalesPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Sales")))
                .ForMember(dest => dest.ReceptionPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Reception")))
                .ForMember(dest => dest.AccountsPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Accounts")))
                .ForMember(dest => dest.Concierges, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Concierge")));

            // HotelStaff -> HotelStaffDto
            CreateMap<HotelStaff, HotelStaffDto>();

            // HotelDto -> HotelInfo
            CreateMap<HotelDto, HotelInfo>()
                .ForMember(dest => dest.HotelStaff, opt => opt.Ignore()); // handled manually in controller

            // HotelStaffDto -> HotelStaff
            CreateMap<HotelStaffDto, HotelStaff>();
        }
    }
}
