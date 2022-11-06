using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Recorder.DirectX;

using Sparin.Screenshot.Interop;

using SkiaSharp;


namespace Recorder
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ShellScalingApi.SetProcessDpiAwareness(ShellScalingApi.PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            var logger = loggerFactory.CreateLogger<DirectXFrameSource>();
            //var s = Path.GetDirectoryName("H:\\Desktop\\ffmpeg-5.1-full_build\\bin\\ffmpeg.exe");

            //var frameSource = GdiFrameSource.Create(loggerFactory.CreateLogger<GdiFrameSource>());
            var frameSource = new DirectXFrameSource(logger);//GdiFrameSource.Create();
            //frameSource.PrintDiagnosticInformation(Console.Out);
            //await SaveVideo(frameSource);
            await SaveJpgs(frameSource);
        }

        private static async Task SaveVideo(IFrameSource frameSource)
        {
            const string outputFilename = "output.mp4";
            if(File.Exists(outputFilename))
                File.Delete(outputFilename);

            var ffmpegStartInfo = new ProcessStartInfo("H:\\Desktop\\ffmpeg-5.1-full_build\\bin\\ffmpeg.exe",
                $"-f rawvideo " +
                $"-pixel_format bgra " +
                $"-video_size {frameSource.Width}x{frameSource.Height} " +
                $"-i pipe:0 " +
                $"-vf \"setpts='(RTCTIME - RTCSTART) / (TB * 1000000)'\" " +
                $"-s {frameSource.Width}x{frameSource.Height} " +
                $"-r 60 " +
                $"-fps_mode cfr " +
                //$"-c:v libx264 -preset superfast " +
                $"{outputFilename}")
            {
                RedirectStandardInput = true
            };

            var ffmpegProcess = Process.Start(ffmpegStartInfo) ?? throw new Exception("ffmpeg didn't started");

            var fps = 1000l / 60;
            var watch = new Stopwatch();
            long start=0, end=0;
            watch.Restart();
            while (!ffmpegProcess.HasExited && ffmpegProcess.StandardInput.BaseStream.CanWrite)
            {
                if (watch.ElapsedMilliseconds < fps - end + start)
                    continue;


                start = watch.ElapsedMilliseconds;
                var rawFrame = frameSource.AcquireNextFrame();
                await ffmpegProcess.StandardInput.BaseStream.WriteAsync(rawFrame);
                end = watch.ElapsedMilliseconds;
                Console.WriteLine(
                    $"Frame rendered in {watch.ElapsedMilliseconds} ms ({(watch.ElapsedMilliseconds != 0 ? 1000 / watch.ElapsedMilliseconds : 0)} FPS)");
                watch.Restart();

            }
        }

        private static unsafe Task SaveJpgs(IFrameSource frameSource)
        {
            const string outputDir = "./output";
            Directory.CreateDirectory(outputDir);

            var skInfo = new SKImageInfo(frameSource.Width, frameSource.Height, SKColorType.Bgra8888);
            var skBitmap = new SKBitmap(skInfo);
            var watch = new Stopwatch();

            watch.Start();
            for (var i = 0; i < 100_000; i++)
            {
                watch.Restart();

                var path = Path.Combine(outputDir, $"{i}.jpg");
                using var fileStream = File.Open(path, FileMode.Create, FileAccess.Write);

                var rawFrame = frameSource.AcquireNextFrame();
                using var rawImageHandle = rawFrame.Pin();

                // SetPixels vs InstallPixels
                // https://stackoverflow.com/questions/61199265/what-is-the-difference-between-installpixels-and-setpixels-in-skiasharp
                skBitmap.SetPixels((IntPtr)rawImageHandle.Pointer);

                skBitmap.Encode(fileStream, SKEncodedImageFormat.Jpeg, 100);

                Console.WriteLine(
                    $"Frame #{i} rendered in {watch.ElapsedMilliseconds} ms ({(watch.ElapsedMilliseconds != 0 ? 1000 / watch.ElapsedMilliseconds : 0)} FPS)");
            }

            return Task.CompletedTask;
        }
    }
}