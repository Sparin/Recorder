using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;

namespace Recorder.DirectX
{
    public sealed unsafe class D3DDeviceContext : IDisposable
    {
        private ID3D11DeviceContext* _deviceContext;

        public D3DDeviceContext(ID3D11DeviceContext* device)
        {
            _deviceContext = (IntPtr)device == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(device))
                : device;
        }

        public void CopyResource(D3DTexture2D destination, D3DTexture2D source) 
            => _deviceContext->CopyResource(destination, source);

        public void CopySubResourceRegion(
            D3DTexture2D dstResource, 
            uint dstX, 
            uint dstY, 
            D3DTexture2D sourceResource, 
            Box sourceBox)
        {
            _deviceContext->CopySubresourceRegion(
                pDstResource: dstResource,
                DstSubresource: 0,
                dstX,
                dstY,
                DstZ: 0,
                sourceResource,
                0,
                &sourceBox);
        }

        private void ReleaseUnmanagedResources()
        {
            _deviceContext->Release();
            _deviceContext = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~D3DDeviceContext()
        {
            ReleaseUnmanagedResources();
        }
    }
}
