using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using WMPLib;

namespace OmegaStreamServices.Data
{
    public class FileManager
    {
        public static async Task SaveStreamToFileAsync(string path, Stream content)
        {
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(fileStream);
        }

        public static FileStream OpenFileStream(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static string GenerateFileName()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
