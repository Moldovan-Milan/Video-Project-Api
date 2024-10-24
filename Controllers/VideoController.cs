using MediaToolkit.Model;
using MediaToolkit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

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
        // Összeállítja a fájlt
        public async Task<IActionResult> AssembleFile([FromForm] string fileName, 
            [FromForm] IFormFile image, [FromForm] int totalChunks, [FromForm] string title)
        {
            var finalPath = Path.Combine("video", fileName);
            // fileName: video.mp4 => [video, mp4]
            string[] videoNameAndExtiension = fileName.Split('.');

            using (var finalStream = new FileStream(finalPath, FileMode.Create))
            {
                // Végigmegy az összes chunkon
                for (int i = 0; i < totalChunks; i++)
                {
                    var chunkPath = Path.Combine(_tempPath, $"{fileName}.part{i}");
                    // Megnyitja a chunkot
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        chunkStream.CopyTo(finalStream); // Átmásolja a chunk tartalmát
                    }
                    System.IO.File.Delete(chunkPath);
                }
            }

            // Indexkép lementése
            if (image != null && image.Length > 0)
            {
                var imagePath = Path.Combine("video/thumbnail/", image.FileName);
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
            }

            // Még hibát dob ki, a fájl nem található 
           // Videó hosszának lekérdezése
           //var inputFile = new MediaFile { Filename = finalPath };
           // using (var engine = new Engine())
           // {
           //     engine.GetMetadata(inputFile);
           // }

           // var duration = inputFile.Metadata.Duration.TotalSeconds;

            // Videó mentése az adatbázisba
            Video video = new Video
            {
                Path = videoNameAndExtiension[0],
                Created = DateTime.Now,
                Duration = 200, // Még nem megy
                Extension = $".{videoNameAndExtiension[1]}",
                ThumbnailPath = image.FileName.Split('.')[0], // image.png => image
                Title = title
            };

            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();

            return Created();
        }
    }
}
