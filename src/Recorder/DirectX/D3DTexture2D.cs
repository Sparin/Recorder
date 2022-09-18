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
    public sealed unsafe class D3DTexture2D : IDisposable
    {
        private ID3D11Texture2D* _texture;

        private Texture2DDesc description;
        public Texture2DDesc Description => description;

        public D3DTexture2D(ID3D11Texture2D* texture)
        {
            _texture = (IntPtr)texture == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(texture))
                : texture;

            UpdateDescription();
        }

        public Surface GetSurface()
        {
            var riid = IDXGISurface1.Guid;
            IDXGISurface1* surface;
            SilkMarshal.ThrowHResult(_texture->QueryInterface(ref riid, (void**)&surface));

            return new Surface(surface);
        }

        private void UpdateDescription() => _texture->GetDesc(ref description);

        public static implicit operator ID3D11Texture2D*(D3DTexture2D? texture) => texture == null ? null : texture._texture;
        public static implicit operator ID3D11Resource*(D3DTexture2D texture) => (ID3D11Resource*)texture._texture;

        private void ReleaseUnmanagedResources()
        {
            if(_texture != null)
                _texture->Release();
            _texture = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~D3DTexture2D()
        {
            ReleaseUnmanagedResources();
        }
    }
}
