namespace AZ_AI_Translate_Document.Models
{
    public class UploadFileModel
    {
        public IFormFile File { get; set; }
        public string targetLanguage { get; set; } = "en";
    }
}
