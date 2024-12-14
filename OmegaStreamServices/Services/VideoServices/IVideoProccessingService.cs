using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoProccessingService
    {
        Task SplitMP4ToM3U8(string inputPath, string outputName, string workingDirectory, int splitTimeInSec = 10);
        List<string> ReadAndChange(string inputFileName);
        Task WriteM3U8File(List<string> lines, string fileName);
        
    }
}
