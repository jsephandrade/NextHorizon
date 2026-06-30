using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Services;

namespace MyAspNetApp.Controllers
{
    public class MediaController : Controller
    {
        private readonly MediaPathService _mediaPathService;

        public MediaController(MediaPathService mediaPathService)
        {
            _mediaPathService = mediaPathService;
        }

        [HttpGet("/Media/File")]
        public IActionResult FileFromPath(string path)
        {
            var localPath = _mediaPathService.ResolveLocalPath(path);
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return NotFound();
            }

            return PhysicalFile(localPath, _mediaPathService.GetContentType(localPath));
        }
    }
}
