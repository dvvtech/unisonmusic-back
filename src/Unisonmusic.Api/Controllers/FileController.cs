using Microsoft.AspNetCore.Mvc;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Controllers
{
    [Route("file")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IOfftubeClient _offtubeClient;
        private readonly IStorageService _storageService;

        public FileController(
            IOfftubeClient offtubeClient, IStorageService storageService)
        {
            _offtubeClient = offtubeClient;
            _storageService = storageService;
        }

        [HttpPost("upload-from-url")]
        public async Task<IActionResult> UploadFromUrl(
            [FromBody] UrlRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Url))
            {
                return BadRequest("Url is required");
            }

            //обращаемся к бд нет ли такой уже ссылки
            //если есть то возвращаем ее key
            //если нет то идем дальше

            var objectKey = await _offtubeClient.GetFileKeyAsync(
                request.Url,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(objectKey))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to upload file");
            }

            //получить ссылку на файл в s3
            var url = _storageService.GetPresignedUrl(objectKey);

            //сохраняем в бд запись s3Url(она временная возможно ее не нужно сохранять), objectKey

            return Ok(new UrlS3Response
            {
                Url = url
            });
        }
    }
}
