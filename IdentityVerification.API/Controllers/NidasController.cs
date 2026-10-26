using IdentityVerification.API.Services.IService;
using Microsoft.AspNetCore.Mvc;
using ApiResponse = Common.DTOs.Response.Api;
using Request = IdentityVerification.API.DTO.Request;

namespace IdentityVerification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NidasController(INidasService nidasService) : ControllerBase
    {
        private readonly INidasService _nidasService = nidasService;

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] Request.VerifyRequest request)
        {
            try
            {
                var (statusCode, rawJson, headers) = await _nidasService.Verify(request);
                return CreateRawJsonResponse(statusCode, rawJson, headers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateInternalErrorResponse(ex.Message));
            }
        }

        [HttpPost("query/qr/check")]
        public async Task<IActionResult> QRQueryCheck([FromBody] Request.QrCheckRequest request)
        {
            try
            {
                var (statusCode, rawJson, headers) = await _nidasService.QRQueryCheck(request);
                return CreateRawJsonResponse(statusCode, rawJson, headers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateInternalErrorResponse(ex.Message));
            }
        }

        [HttpPost("query/qr")]
        public async Task<IActionResult> QRQueryVerify([FromBody] Request.QrVerifyRequest request)
        {
            try
            {
                var (statusCode, rawJson, headers) = await _nidasService.QRQueryVerify(request);
                return CreateRawJsonResponse(statusCode, rawJson, headers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateInternalErrorResponse(ex.Message));
            }
        }

        #region Helper Methods

        private IActionResult CreateRawJsonResponse(int statusCode, string rawJson, Dictionary<string, string[]> headers)
        { 
            var excludedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Transfer-Encoding",
                "Content-Length",
                "Content-Type",
                "Server"
            };

            foreach (var (key, values) in headers)
            {
                if (!excludedHeaders.Contains(key))
                {
                    Response.Headers[key] = values;
                }
            }

            return new ContentResult
            {
                StatusCode = statusCode,
                Content = rawJson,
                ContentType = "application/json"
            };
        }

        private static ApiResponse.Response<object> CreateInternalErrorResponse(string errorMessage)
        {
            return new ApiResponse.Response<object>
            {
                Success = false,
                Message = "An unexpected system error occurred while processing the request.",
                Errors =
                [
                    new ApiResponse.Error
                    {
                        Message = errorMessage
                    }
                ]
            };
        }

        #endregion
    }
}