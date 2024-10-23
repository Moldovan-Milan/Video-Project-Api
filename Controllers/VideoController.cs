using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const int ChunkSize = 10 * 1024 * 1024; // A videó chunkolási mérete
        private readonly IMemoryCache _cache;

        public VideoController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: /api/video/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            // Ha le van mentve a cache-be akkor nem kéri le az adatokat
            if (!_cache.TryGetValue(id, out var video))
            {
                video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == id);
                if (video == null)
                    return NotFound();
                _cache.Set(id, video, TimeSpan.FromMinutes(30)); // Cache lejárati idő beállítása
            }

            return await StreamVideo((Video)video);
        }

        // A videó streamelés logikája
        private async Task<IActionResult> StreamVideo(Video video)
        {
            string fullPath = Path.Combine("video", $"{video.Path}{video.Extension}");

            string range = Request.Headers.Range.ToString(); // A kért tartalom
            // Ha van range, akkor az alapján ad vissza
            if (!string.IsNullOrEmpty(range))
            {
                FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read);

                return File(fileStream, "video/mp4", enableRangeProcessing: true);
            }
            // Ha nincs, akkor az egész videót elküldi
            else
            {
                FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                return File(fileStream, "video/mp4"); // Visszaadott fájl + MIME típus
            }

        }

        // A videók betöltése
        [HttpGet]
        public async Task<IActionResult> GetVideos()
        {
            List<Video> videos = await _context.Videos.ToListAsync();
            if (videos == null)
                return NotFound();
            return Ok(videos);
        }
        [Route("thumbnail/{name}")]
        [HttpGet]
        public async Task<IActionResult> GetThumbnailImage(string name)
        {
            string fullPath = Path.Combine("video/thumbnail", $"{name}.png");
            FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                   FileAccess.Read, FileShare.Read);
            return File(fileStream, "image/png");
        }
    }
}
