using Microsoft.AspNetCore.Http;
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

        public FileController(IOfftubeClient offtubeClient)
        {
            _offtubeClient = offtubeClient;
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

            var key = await _offtubeClient.GetFileKeyAsync(
                request.Url,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(key))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to upload file");
            }

            return Ok(new UploadResponse
            {
                Key = key
            });
        }
    }
}
