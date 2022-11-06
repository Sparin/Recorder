using Recorder.Direct3D11.Interop.Direct3D11;
using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder.Direct3D11.Interop.Direct3D
{
    public sealed unsafe class RenderTargetView : IDisposable
    {
        private ID3D11RenderTargetView* _deviceContext;

        public RenderTargetView(ID3D11RenderTargetView* device)
        {
            _deviceContext = (IntPtr)device == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(device))
                : device;
        }

        private void ReleaseUnmanagedResources()
        {
            if (_deviceContext != null)
                _deviceContext->Release();
            _deviceContext = null;
        }

        public static implicit operator ID3D11RenderTargetView*(RenderTargetView rtv) => rtv._deviceContext;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RenderTargetView()
        {
            ReleaseUnmanagedResources();
        }
    }
}
