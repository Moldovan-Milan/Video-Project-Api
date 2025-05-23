﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public interface ICloudService
    {
        Task<long> GetBucketFileSizeSum();
        Task UploadToR2(string key, Stream content);
        Task<(Stream stream, string contentType)> GetFileStreamAsync(string key);
        Task DeleteFilesAsync(string path);
        Task DeleteFileAsync(string filePath);
    }
}
