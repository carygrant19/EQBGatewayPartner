using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class CategoryService(EFDbContext efDbContext, IMapper mapper, ILogService logService) : ICategoryService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IMapper _mapper = mapper;
        private readonly ILogService _logService = logService;
        private readonly string _moduleName = "Category";

        public async Task<List<Response.Category>> GetAllActiveAsync()
        {
            try
            {
                var data = await _efDbContext.Set<Category>()
                    .Where(c => c.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                return _mapper.Map<List<Response.Category>>(data);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
    }
}