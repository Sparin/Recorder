using Recorder.DirectX;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorder.Direct3D11.Interop.Direct3D11;

namespace Recorder.Direct3D11.Interop.Direct3D
{
    public sealed unsafe class ShaderResourceView : IDisposable
    {
        private ID3D11ShaderResourceView* _shaderResourceView;

        public ShaderResourceView(ID3D11ShaderResourceView* texture)
        {
            _shaderResourceView = (IntPtr)texture == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(texture))
                : texture;
        }

        public static implicit operator ID3D11ShaderResourceView*(ShaderResourceView shaderResourceView) => shaderResourceView._shaderResourceView;

        private void ReleaseUnmanagedResources()
        {
            if (_shaderResourceView != null)
                _shaderResourceView->Release();
            _shaderResourceView = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ShaderResourceView()
        {
            ReleaseUnmanagedResources();
        }
    }
}
