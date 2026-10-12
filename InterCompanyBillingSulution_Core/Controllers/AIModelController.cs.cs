
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIModelController : ControllerBase
    {
        private readonly PythonAIService _pythonAIService;
        private readonly DataQueryService _dataQueryService;

        public AIModelController(PythonAIService pythonAIService, DataQueryService dataQueryService)
        {
            _pythonAIService = pythonAIService;
            _dataQueryService = dataQueryService;
        }

        // Open to signed-out users too; database answers below are only given to signed-in users.
        [AllowAnonymous]
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
                // Ask the model what the question is about; answer from the DB if it is a data question.
                string? dbAnswer = null;
                if (User.Identity?.IsAuthenticated == true && DataQueryService.AsksForData(request.Message))
                {
                    var query = DataQueryService.TryParse(request.Message)
                                ?? await _pythonAIService.ClassifyAsync(request.Message);
                    dbAnswer = query == null ? null : await _dataQueryService.AnswerAsync(query);
                }

                // Otherwise (e.g. "how to generate a quotation") answer straight from the help text;
                // only fall back to the (slow) LLM when no help screen matches.
                var response = dbAnswer ?? await _dataQueryService.GetHelpAnswerAsync(request.Message);
                if (response == null)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(240));
                    try
                    {
                        response = await _pythonAIService.GenerateResponse(
                            request.Message, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        response = "I couldn't find an answer to that in the application help, and the AI model took too long to respond. Please rephrase your question, e.g. \"How to create a quotation?\" or \"How many approved invoices?\"";
                    }
                }

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