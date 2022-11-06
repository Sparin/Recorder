using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorder.DirectX;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Recorder.Direct3D11.Interop.Direct3D11
{
    public sealed unsafe class Texture2D : IDisposable
    {
        private ID3D11Texture2D* _texture;

        private Texture2DDesc description;
        public Texture2DDesc Description => description;

        public Texture2D(ID3D11Texture2D* texture)
        {
            _texture = (IntPtr)texture == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(texture))
                : texture;

            //UpdateDescription();
        }

        public Surface1 GetSurface()
        {
            var riid = IDXGISurface1.Guid;
            IDXGISurface1* surface;
            SilkMarshal.ThrowHResult(_texture->QueryInterface(ref riid, (void**)&surface));

            return new Surface1(surface);
        }

        private void UpdateDescription() => _texture->GetDesc(ref description);

        public static implicit operator ID3D11Texture2D*(Texture2D? texture) => texture == null ? null : texture._texture;
        public static implicit operator ID3D11Resource*(Texture2D texture) => (ID3D11Resource*)texture._texture;

        private void ReleaseUnmanagedResources()
        {
            if (_texture != null)
                _texture->Release();
            _texture = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Texture2D()
        {
            ReleaseUnmanagedResources();
        }
    }
}
