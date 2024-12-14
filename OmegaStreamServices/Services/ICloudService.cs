using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public interface ICloudService
    {
        Task UploadToR2(string key, Stream content);
        Task<(Stream stream, string contentType)> GetFileStreamAsync(string key);
    }
}
