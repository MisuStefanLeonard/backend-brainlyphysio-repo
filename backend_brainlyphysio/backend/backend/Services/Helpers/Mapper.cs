// using AutoMapper;
// using backend.Models.DTO;
// using backend.Models.DTO.LocationDto;
// using backend.Models.DTO.QualityDto;
// using backend.Models.User;
//
// namespace backend.Services.Helpers;
//
// public class Mapper : Profile
// {
//     public Mapper()
//     {
//         CreateMap<AccountCrud, Account>()
//             .ForMember(dest => dest.Name, from => from.MapFrom(src => src.Name))
//             .ForMember(dest => dest.Prename, from => from.MapFrom(src => src.Prename))
//             .ForMember(dest => dest.Email, from => from.MapFrom(src => src.Email))
//             .ForMember(dest => dest.Description, from => from.MapFrom(src => src.Description))
//             .ForMember(dest => dest.PhoneNumber, from => from.MapFrom(src => src.PhoneNumber));
//
//
//         CreateMap<LocationCrud, Location>()
//             .ForMember(dest => dest.City, from => from.MapFrom(src => src.City))
//             .ForMember(dest => dest.County, from => from.MapFrom(src => src.County));
//
//
//         CreateMap<QualityCrud, Quality>()
//             .ForMember(dest => dest.QualityName, from => from.MapFrom(src => src.QualityName));
//     }
// }