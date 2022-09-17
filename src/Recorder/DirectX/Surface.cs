using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Recorder.DirectX
{
    public sealed unsafe class Surface : IDisposable
    {
        private IDXGISurface1* _surface;

        private bool isMapped = false;
        private MappedRect _rectangle;

        public Surface(IDXGISurface1* surface)
        {
            _surface = surface == null
                ? throw new ArgumentNullException(nameof(surface))
                : surface;
        }

        public MappedRect Map(uint flags)
        {
            if (isMapped)
                return _rectangle;

            _rectangle = new MappedRect();
            var hresult = _surface->Map(ref _rectangle, (uint)flags);
            SilkMarshal.ThrowHResult(hresult);
            isMapped = true;
            return _rectangle;
        }

        public void Unmap()
        {
            SilkMarshal.ThrowHResult(_surface->Unmap());
            isMapped = false;
        }

        private void ReleaseUnmanagedResources()
        {
            _surface->Release();
            _surface = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Surface()
        {
            ReleaseUnmanagedResources();
        }
    }
}
