using AutoMapper;
using Azure.Core;
using Gateway.BLL.Helper; 
using Microsoft.Extensions.Configuration; 
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL
{
    public class MappingProfile : Profile
    { 
        public MappingProfile(IConfiguration configuration)
        {
            var encryptionKey = configuration["AppContext:EncryptionKey"]!;
             
            CreateMap<Model.Client, Response.Client>();
            CreateMap<Model.Client, Response.FClient>();

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


            // Core Route mapping configuration
            CreateMap<Request.Route, Model.Route>();

            // Child collection mappings (so AutoMapper can resolve nested lists automatically)
            CreateMap<Request.RouteHost, Model.RouteHost>();
            CreateMap<Request.RouteIpRule, Model.RouteIpRule>();
            CreateMap<Request.RouteClient, Model.RouteClient>();
        }
    }
}
