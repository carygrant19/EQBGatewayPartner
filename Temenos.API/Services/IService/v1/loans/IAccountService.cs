using Response = Temenos.API.DTOs.Response.v1.loans;
using ApiResponse = Common.DTOs.Response.Api;

namespace Temenos.API.Services.IService.v1.loans
{
    public interface IAccountService
    {
        Task<ApiResponse.Response<List<Response.AmortizationSchedule>>> AmortizationSchedule(string arrangementId);
    }
}
