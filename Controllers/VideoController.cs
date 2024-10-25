using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;
using WMPLib;

namespace VideoProjektAspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        // Ide menti az ideiglenes videó chunkokat a feltöltés során
        private readonly string _tempPath = Path.Combine("temp");

        private readonly string _videoPath = Path.Combine("video");

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
            string fullPath = Path.Combine(_videoPath, $"{video.Path}.{video.Extension}");

            string range = Request.Headers.Range.ToString(); // A kért tartalom
            // Ha van range, akkor az felbontva ad vissza
            if (!string.IsNullOrEmpty(range))
            {
                FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read);

                return File(fileStream, $"video/{video.Extension}", enableRangeProcessing: true);
            }
            // Ha nincs, akkor az egész videót elküldi
            else
            {
                FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                return File(fileStream, $"video/{video.Extension}"); // Visszaadott fájl + MIME típus
            }

        }

        // A videók adatainak betöltése
        [HttpGet]
        public async Task<IActionResult> GetVideosData()
        {
            List<Video> videos = await _context.Videos.ToListAsync();
            if (videos == null)
                return NotFound();
            return Ok(videos);
        }

        // Egy videó adatainak betöltése
        [Route("data/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetVideoData(int id)
        {
            Video video = await _context.Videos.FirstOrDefaultAsync(x => x.Id == id);
            if (video == null) 
                return NotFound();
            return Ok(video);
        }

        [Route("thumbnail/{name}")]
        [HttpGet]
        // Az indexképet küldi vissza
        public async Task<IActionResult> GetThumbnailImage(string name)
        {
            string fullPath = Path.Combine("video/thumbnail", $"{name}.png");
            FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                   FileAccess.Read, FileShare.Read);
            return File(fileStream, "image/png");
        }

        #region UPLOAD VIDEO
        [Route("upload")]
        [HttpPost]
        // A chunkokat ideiglenesen elmenti a temp mappába
        public async Task<IActionResult> UploadChunl([FromForm] IFormFile chunk, 
            [FromForm] string fileName, [FromForm] int chunkNumber)
        {
            if (chunk == null || chunk.Length == 0)
            {
                return BadRequest("No chunk uploaded.");
            }

            var chunkPath = Path.Combine(_tempPath, $"{fileName}.part{chunkNumber}");
            using (FileStream stream = new FileStream(chunkPath, FileMode.Create))
            {
                await chunk.CopyToAsync(stream); // Átmásolja a chunk tartalmát a fájlba.
            }

            return Created();
        }

        [Route("assemble")]
        [HttpPost]
        // Összeállítja a videó fájlt
        public async Task<IActionResult> AssembleFile([FromForm] string fileName, 
            [FromForm] IFormFile image, [FromForm] int totalChunks, [FromForm] string title,
            [FromForm] string extension)
        {
            // Egyedi név adása a videónak és az indexképnek
            string uniqueFileName = GenerateUniqueFileName();
            var finalPath = Path.Combine(_videoPath, $"{uniqueFileName}.{extension}");

            await AssembleChunksToFile(finalPath, fileName, totalChunks);
            SaveThumbnail(image, uniqueFileName);

            TimeSpan duration = GetVideoDuration(finalPath);
            await SaveVideoToDatabase(uniqueFileName, duration, extension, title);

            return Created();
        }

        // Chunkok összeállítása a videó fájlba
        private async Task AssembleChunksToFile(string finalPath, string fileName, int totalChunks)
        {
            using (var finalStream = new FileStream(finalPath, FileMode.Create))
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    var chunkPath = Path.Combine(_tempPath, $"{fileName}.part{i}");
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await chunkStream.CopyToAsync(finalStream);
                    }
                    System.IO.File.Delete(chunkPath);
                }
            }
        }

        private void SaveThumbnail(IFormFile image, string uniqueFileName)
        {
            if (image != null && image.Length > 0)
            {
                var imagePath = Path.Combine("video/thumbnail/", $"{uniqueFileName}.png");
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }
            }
        }

        // Egyedi fájlnév generálása a videónak és az indexképnek
        private string GenerateUniqueFileName()
        {
            string uniqueFileName;
            do
            {
                uniqueFileName = Guid.NewGuid().ToString();
            } while (_context.Videos.Any(x => x.Path == uniqueFileName));
            return uniqueFileName;
        }

        private TimeSpan GetVideoDuration(string finalPath)
        {
            WindowsMediaPlayer wmp = new WindowsMediaPlayer();
            IWMPMedia mediaInfo = wmp.newMedia(finalPath);
            return TimeSpan.FromSeconds(mediaInfo.duration);
        }

        private async Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title)
        {
            Video video = new Video
            {
                Path = uniqueFileName,
                Created = DateTime.Now,
                Duration = duration,
                Extension = videoExtension.Split(".")[0],
                ThumbnailPath = uniqueFileName,
                Title = title
            };

            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();
        }
        #endregion

        // Elmenti az indexképet jpg formátumba
        // TODO: Megcsinálni a jpeg konvertálást
        private bool ConvertThumbnailToJpg(IFormFile image) => throw new NotImplementedException();
    }
}
