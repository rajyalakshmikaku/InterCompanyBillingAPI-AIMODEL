
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
                var message = request.Message.Trim();
                string? response = null;

                // 1) Data questions (counts, totals, latest/highest...) are answered from the database,
                //    for signed-in users only. The model only classifies; it never writes SQL.
                //    Follow-ups such as "give those users names" are resolved against the previous question.
                var withContext = DataQueryService.WithContext(message, request.History);
                if (User.Identity?.IsAuthenticated == true && DataQueryService.AsksForData(withContext))
                {
                    try
                    {
                        var query = DataQueryService.TryParse(message) ?? await _pythonAIService.ClassifyAsync(withContext);
                        response = query == null ? null : await _dataQueryService.AnswerAsync(query);
                    }
                    catch (Exception)
                    {
                        // Classification/DB problem: fall through and let the model answer generally.
                    }
                }

                // 2) Everything else - how-to, concepts, advanced usage or general questions - is answered by the model,
                //    grounded in the application overview and the best-matching help screens.
                if (response == null)
                {
                    var (prompt, helpOnly, isApp) = await _dataQueryService.BuildKnowledgePromptAsync(message);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(240));
                    try
                    {
                        // General questions (not about the billing app) use a plain prompt with no application context.
                        response = await _pythonAIService.GenerateResponse(prompt, cts.Token, general: !isApp);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
                    {
                        // AI server unavailable: still give the user the help text if we have it.
                        response = helpOnly ?? "The AI assistant is not reachable right now. Please try again in a moment.";
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