using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;
using VideoProjektAspApi.Services;
using WMPLib;

namespace VideoProjektAspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Services
        private readonly IVideoUploadService _videoUploadService;
        private readonly IVideoStreamService _videoStreamService;

        public VideoController(AppDbContext context,
            IVideoUploadService videoUploadService, IVideoStreamService videoStreamService)
        {
            _context = context;
            _videoUploadService = videoUploadService;
            _videoStreamService = videoStreamService;
        }

        // GET: /api/video/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            Video video = await _videoStreamService.GetVideoData(id);
            if (video == null)
                return NotFound();

            FileStream videoStream = _videoStreamService.StreamVideo(video);
            string range = Request.Headers.Range;
            if (string.IsNullOrEmpty(range))
                return File(videoStream, $"video/{video.Extension}"); // A teljes videó visszaadása


            return File(videoStream, $"video/{video.Extension}", enableRangeProcessing: true);
        }
     

        // A videók adatainak betöltése
        [HttpGet]
        public async Task<IActionResult> GetVideosData()
        {
            List<Video> videos = await _videoStreamService.GetAllVideosData();
            
            return videos == null ? NotFound() : Ok(videos);
        }

        // Egy videó adatainak betöltése
        [Route("data/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetVideoData(int id)
        {
            Video video = await _context.Videos.FirstOrDefaultAsync(x => x.Id == id);

            return video == null ? NotFound() : Ok(video);
        }

        [Route("thumbnail/{name}")]
        [HttpGet]
        // Az indexképet küldi vissza
        public async Task<IActionResult> GetThumbnailImage(string name)
        {
            FileStream imageStream = _videoStreamService.GetThumbnailImage(name);

            return imageStream == null ? NotFound() : File(imageStream, "image/png");
        }

        [Route("upload")]
        [HttpPost]
        // A chunkokat ideiglenesen elmenti a temp mappába
        public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk, 
            [FromForm] string fileName, [FromForm] int chunkNumber)
        {
            if (chunk == null || chunk.Length == 0)
                return BadRequest("No chunk uploaded.");

            await _videoUploadService.UploadChunk(chunk, fileName, chunkNumber);
            return Created();
        }

        [Route("assemble")]
        [HttpPost]
        // Összeállítja a videó fájlt
        public async Task<IActionResult> AssembleFile([FromForm] string fileName, 
            [FromForm] IFormFile image, [FromForm] int totalChunks, [FromForm] string title,
            [FromForm] string extension)
        {
            await _videoUploadService.AssembleFile(fileName, image, totalChunks, title, extension);
            return Created();
        }
    }
}
