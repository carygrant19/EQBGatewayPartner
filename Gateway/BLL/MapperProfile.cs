using AutoMapper;
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
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => StringManipulation.Encrypt(src.Id.ToString(), encryptionKey)))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId.ToString()))
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : ""))
                .ForMember(dest => dest.ModulePermission, opt => opt.MapFrom(src => src.ModulePermission != null ? string.Join("|", src.ModulePermission.Select(mp => mp.PermissionId.ToString())) : ""));

            CreateMap<Model.Permission, Response.FPermission>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => StringManipulation.Encrypt(src.Id.ToString(), encryptionKey)));

            CreateMap<Model.Category, Response.Category>();
            CreateMap<Request.Category, Model.Category>();

            CreateMap<Request.TargetHost, Model.TargetHost>();
            CreateMap<Model.TargetHost, Response.FTargetHost>()
                .ForMember(dest => dest.EndpointId, opt => opt.MapFrom(src => src.RouteId.ToString()));

            CreateMap<Request.RouteIpRule, Model.RouteIpRule>();
            CreateMap<Model.RouteIpRule, Response.FRouteIpRule>()
                .ForMember(dest => dest.EndpointId, opt => opt.MapFrom(src => src.RouteId.ToString()));

            CreateMap<Request.Route, Model.Route>();
            CreateMap<Model.Route, Response.Route>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Uncategorized"));
            CreateMap<Model.Route, Response.FRoute>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Uncategorized"));

            CreateMap<Model.Route, Response.Route>()
                .ForMember(dest => dest.OutboundAuthProfileName, opt => opt.MapFrom(src => src.OutboundAuthProfile != null ? src.OutboundAuthProfile.Name : null));
            CreateMap<Request.Client, Model.Client>();
        }
    }
}