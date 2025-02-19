using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMPLib;

namespace OmegaStreamServices.Data
{
    public class VideoSplitter
    {
        public static async Task SplitMP4ToM3U8(string inputPath, string outputName, string workingDirectory, int splitTimeInSec = 10)
        {
            // ffmpeg parancsot futtat cmd-ben, amely felbontja a videót több .ts fájlra
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments =
                    $"-i \"{inputPath}\" -codec: copy -start_number 0 -hls_time {splitTimeInSec} -hls_list_size 0 -hls_segment_filename \"{outputName}%03d.ts\" -f hls \"{outputName}.m3u8\"";
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"ERROR: {e.Data}"); };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                var lines = ReadAndChange($"{workingDirectory}/{outputName}.m3u8");
                WriteM3U8File(lines, $"{workingDirectory}/{outputName}.m3u8");
            }

        }

        private static List<string> ReadAndChange(string inputFileName)
        {
            // Egy string listát ad vissza, amibe beírja az elérési útvonalát, amellyel
            // a kliens le tudja kérdezni a .ts fájlt a szerverről
            // pl.: /api/video/segments/'fajlnev'.ts
            List<string> result = new List<string>();
            StreamReader streamReader = new StreamReader(inputFileName);

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine()!;
                if (line.Contains(".ts"))
                {
                    string changedLine = $"/api/video/segments/{line}";
                    result.Add(changedLine);
                }
                else
                {
                    result.Add(line);
                }
            }
            streamReader.Close();

            return result;
        }
        private static void WriteM3U8File(List<string> lines, string fileName)
        {
            // Ez fogja bemásolni az átírt sorokat a .m3u8 fájlba
            StreamWriter streamWriter = new StreamWriter(fileName);
            foreach (string line in lines)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }
    }
}
