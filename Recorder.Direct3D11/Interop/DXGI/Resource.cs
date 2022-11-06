using Recorder.Direct3D11.Interop.Direct3D11;
using Recorder.DirectX;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder.Direct3D11.Interop.DXGI
{
    public sealed unsafe class Resource : IDisposable
    {
        private IDXGIResource* _resource;

        public Resource(IDXGIResource* surface)
        {
            _resource = surface == null
                ? throw new ArgumentNullException(nameof(surface))
                : surface;
        }

        public static implicit operator Texture2D(Resource resource)
        {
            var riid = ID3D11Texture2D.Guid;
            ID3D11Texture2D* texture;
            SilkMarshal.ThrowHResult(resource._resource->QueryInterface(ref riid, (void**)&texture));
            return new Texture2D(texture);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_resource != null)
                _resource->Release();
            _resource = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Resource()
        {
            ReleaseUnmanagedResources();
        }
    }
}
