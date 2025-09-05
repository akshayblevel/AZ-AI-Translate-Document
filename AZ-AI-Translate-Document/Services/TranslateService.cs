using AZ_AI_Translate_Document.Interfaces;
using Azure;
using Azure.AI.Translation.Document;
using Azure.Storage.Blobs;

namespace AZ_AI_Translate_Document.Services
{
    public class TranslateService : ITranslateService
    {
        private readonly DocumentTranslationClient _client;
        private readonly BlobContainerClient _sourceContainer;
        private readonly BlobContainerClient _targetContainer;

        public TranslateService(IConfiguration config)
        {
            string key = config["DocumentTranslator:Key"]!;
            string endpoint = config["DocumentTranslator:Endpoint"]!;
            string connStr = config["Storage:ConnectionString"]!;
            string sourceContainer = config["Storage:SourceContainer"]!;
            string targetContainer = config["Storage:TargetContainer"]!;

            _sourceContainer = new BlobContainerClient(connStr, sourceContainer);
            _targetContainer = new BlobContainerClient(connStr, targetContainer);
            _client = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        public async Task<byte[]> TranslateDocument(IFormFile file, string targetLanguage)
        {
            Uri sourceSas = _sourceContainer.GenerateSasUri(Azure.Storage.Sas.BlobContainerSasPermissions.Write | Azure.Storage.Sas.BlobContainerSasPermissions.Read | Azure.Storage.Sas.BlobContainerSasPermissions.List, DateTimeOffset.UtcNow.AddHours(1));
            Uri targetSas = _targetContainer.GenerateSasUri(Azure.Storage.Sas.BlobContainerSasPermissions.Write | Azure.Storage.Sas.BlobContainerSasPermissions.Read | Azure.Storage.Sas.BlobContainerSasPermissions.List, DateTimeOffset.UtcNow.AddHours(1));

            string sourceBlobName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            BlobClient sourceBlob = _sourceContainer.GetBlobClient(sourceBlobName);
            using (var stream = file.OpenReadStream())
            {
                var sourceBlobUri = await UploadFileToSourceAsync(file, sourceSas.ToString());
            }

            var input = new DocumentTranslationInput(sourceSas, targetSas, targetLanguage);

            var operation = await _client.StartTranslationAsync(input);
            await operation.WaitForCompletionAsync();

            await foreach (var doc in operation.GetValuesAsync())
            {
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(doc.TranslatedDocumentUri);
                return fileBytes;
            }

            return default;
          }

        private async Task<Uri> UploadFileToSourceAsync(IFormFile file, string sourceSas)
        {
            var blobUri = BuildBlobUri(sourceSas, file.FileName);
            using var stream = file.OpenReadStream();
            using var httpClient = new HttpClient();

            var putRequest = new HttpRequestMessage(HttpMethod.Put, blobUri)
            {
                Content = new StreamContent(stream)
            };
            putRequest.Content.Headers.Add("x-ms-blob-type", "BlockBlob");

            var response = await httpClient.SendAsync(putRequest);
            response.EnsureSuccessStatusCode();

            return blobUri;
        }

        public static Uri BuildBlobUri(string containerSasUri, string fileName)
        {
            var encodedFileName = Uri.EscapeDataString(fileName);
            
            var uri = new Uri(containerSasUri);
            var baseUri = uri.GetLeftPart(UriPartial.Path);   
            var query = uri.Query;                            

            var blobUri = $"{baseUri}/{encodedFileName}{query}";
            return new Uri(blobUri);
        }
    }
}
