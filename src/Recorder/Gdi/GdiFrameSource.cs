using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Recorder.Gdi
{
    public sealed class GdiFrameSource : IFrameSource, IDisposable
    {
        public int Height { get; }
        public int Width { get; }

        private readonly Bitmap _bitmap;
        private readonly Graphics _graphics;
        private readonly Rectangle _region;
        private readonly byte[] _buffer;
        private readonly ILogger<GdiFrameSource> _logger;


        private GdiFrameSource(int width, int height, ILogger<GdiFrameSource> logger)
        {
            Height = height;
            Width = width;

            _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _graphics = Graphics.FromImage(_bitmap);
            _region = new Rectangle(0, 0, width, height);
            _buffer = new byte[width * height*4];

            _logger = logger; 
        }

        public ReadOnlyMemory<byte> AcquireNextFrame()
        {
            _graphics.CopyFromScreen(0, 0,0,0, _bitmap.Size, CopyPixelOperation.SourceCopy);

            var data = _bitmap.LockBits(_region, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(data.Scan0, _buffer, 0, _buffer.Length);
            _bitmap.UnlockBits(data);

            return _buffer.AsMemory();
        }

        public static GdiFrameSource Create(ILogger<GdiFrameSource>? logger = null)
        {
            logger ??= NullLogger<GdiFrameSource>.Instance;

            var virtualScreen = SystemInformation.VirtualScreen;
            return new GdiFrameSource(virtualScreen.Width, virtualScreen.Height, logger);
        }

        public void Dispose()
        {
            _graphics.Dispose();
            _bitmap.Dispose();
        }
    }
}
