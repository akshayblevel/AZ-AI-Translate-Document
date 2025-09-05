using AZ_AI_Translate_Document.Interfaces;
using AZ_AI_Translate_Document.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AZ_AI_Translate_Document.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslatorController(ITranslateService translateService) : ControllerBase
    {
        [HttpPost("Document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SentimentAnalysis([FromForm] UploadFileModel model)
        {
            var result = await translateService.TranslateDocument(model.File,model.targetLanguage);
            return Ok();
        }
    }
}
