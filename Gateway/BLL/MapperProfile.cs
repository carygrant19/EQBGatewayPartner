using AutoMapper;
using Gateway.BLL.Helper; 
using Microsoft.Extensions.Configuration; 
using Model = Gateway.Data.Models;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL
{
    public class MappingProfile : Profile
    { 
        public MappingProfile(IConfiguration configuration)
        {
            var encryptionKey = configuration["AppContext:EncryptionKey"]!;
             
            CreateMap<Model.ApiClient, Response.ApiClient>();
            CreateMap<Model.ApiClient, Response.FApiClient>();

            CreateMap<Model.User, Response.User>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => StringManipulation.Encrypt(src.Id.ToString(), encryptionKey)));
            CreateMap<Model.User, Response.FUser>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => StringManipulation.Encrypt(src.Id.ToString(), encryptionKey)));

            CreateMap<Model.Module, Response.FModule>()
                    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => StringManipulation.Encrypt(src.Id.ToString(), configuration["AppContext:EncryptionKey"]!)))
                    .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId.ToString()))
                    .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : ""))
                    .ForMember(dest => dest.ModulePermission, opt => opt.MapFrom(src => src.ModulePermission != null ? string.Join("|", src.ModulePermission.Select(mp => mp.PermissionId.ToString())) : ""));

            CreateMap<Model.Permission, Response.FPermission>().ForMember(dest => dest.Id, opt => opt.MapFrom(src => StringManipulation.Encrypt(src.Id.ToString(), configuration["AppContext:EncryptionKey"]!)));
        }
    }
}
