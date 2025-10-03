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
            // ------------------ Hotel Mappings ------------------
            CreateMap<HotelInfo, HotelDto>()
                .ForMember(dest => dest.SalesPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Sales")))
                .ForMember(dest => dest.ReceptionPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Reception")))
                .ForMember(dest => dest.AccountsPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Accounts")))
                .ForMember(dest => dest.Concierges, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Concierge")))
                .ForMember(dest => dest.ReservationPersons, opt => opt.MapFrom(src => src.HotelStaff.Where(s => s.Role == "Reservation")));

            CreateMap<HotelStaff, HotelStaffDto>();
            CreateMap<HotelDto, HotelInfo>()
                .ForMember(dest => dest.HotelStaff, opt => opt.Ignore());
            CreateMap<HotelStaffDto, HotelStaff>();

            // ------------------ Supplier Mappings ------------------

            // SupplierRequestDto -> Supplier
            CreateMap<SupplierRequestDto, Supplier>()
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.PhoneNo))
                .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.EmailId))

                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt =>
                    opt.Condition((src, dest, srcMember) => src.IsActive.HasValue));

            // Supplier -> SupplierResponseDto
            CreateMap<Supplier, SupplierResponseDto>()
                .ForMember(dest => dest.PhoneNo, opt => opt.MapFrom(src => src.ContactPhone))
                .ForMember(dest => dest.EmailId, opt => opt.MapFrom(src => src.ContactEmail))
                .ForMember(dest => dest.SupplierCategoryName,
                    opt => opt.MapFrom(src => src.SupplierCategory != null ? src.SupplierCategory.Name : null))
                .ForMember(dest => dest.SupplierSubCategoryName,
                    opt => opt.MapFrom(src => src.SupplierSubCategory != null ? src.SupplierSubCategory.Name : null))
                .ForMember(dest => dest.CountryName,
                    opt => opt.MapFrom(src => src.Country != null ? src.Country.Name : null))
                .ForMember(dest => dest.CityName,
                    opt => opt.MapFrom(src => src.City != null ? src.City.Name : null))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
                

            // SupplierCategory <-> SupplierCategoryDto
            CreateMap<SupplierCategory, SupplierCategoryDto>().ReverseMap();
            // SupplierSubCategory <-> SupplierSubCategoryDto
            CreateMap<SupplierSubCategory, SupplierSubCategoryDto>().ReverseMap();
        }
    }
}
