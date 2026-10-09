
//using InterCompanyBillingSulution.DataAcess.Data;
//using InterCompanyBillingSulution.Models;
//using InterCompanyBillingSulution_Core.Services;
//using Microsoft.AspNetCore.Mvc;

//namespace InterCompanyBillingSulution_Core.Controllers
//{
//    public class AIModelController : Controller
//    {

//        private readonly PythonAIService _pythonAIService;
//        public AIModelController(PythonAIService PythonAIService)
//        {

//            _pythonAIService = PythonAIService;
//        }
//        [HttpPost("chat")]
//        public async Task<IActionResult> Chat(
//          [FromBody] ChatRequest request)
//        {
//            if (string.IsNullOrWhiteSpace(request.Message))
//            {
//                return BadRequest("Message is required.");
//            }

//            var response =
//                await _pythonAIService.GenerateResponse(request.Message);

//            return Ok(new ChatResponse
//            {
//                Response = response
//            });
//        }
//    }
//}
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution_Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIModelController : ControllerBase
    {
        private readonly PythonAIService _pythonAIService;

        public AIModelController(PythonAIService pythonAIService)
        {
            _pythonAIService = pythonAIService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Message is required."
                });
            }

            try
            {
                var response = await _pythonAIService.GenerateResponse(
                    request.Message
                );

                return Ok(new
                {
                    success = true,
                    response = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}