namespace AZ_AI_Translate_Document.Interfaces
{
    public interface ITranslateService
    {
        Task<byte[]> TranslateDocument(IFormFile file, string targetLanguage);
    }
}
