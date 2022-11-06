using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder.Direct3D11.Interop.Direct3D
{

    public sealed unsafe class PixelShader : IDisposable
    {
        private ID3D11PixelShader* _pixelShader;

        public PixelShader(ID3D11PixelShader* device)
        {
            _pixelShader = (IntPtr)device == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(device))
                : device;
        }

        private void ReleaseUnmanagedResources()
        {
            if (_pixelShader != null)
                _pixelShader->Release();
            _pixelShader = null;
        }

        public static implicit operator ID3D11PixelShader*(PixelShader pixelShader) => pixelShader._pixelShader;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PixelShader()
        {
            ReleaseUnmanagedResources();
        }
    }
}
