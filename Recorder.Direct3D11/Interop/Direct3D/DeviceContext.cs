using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorder.Direct3D11.Interop.Direct3D;
using Silk.NET.Core.Native;
using Recorder.Direct3D11.Interop.DXGI;
using Buffer = Recorder.Direct3D11.Interop.Direct3D.Buffer;

namespace Recorder.Direct3D11.Interop.Direct3D11
{
    public sealed unsafe class DeviceContext : IDisposable
    {
        private ID3D11DeviceContext* _deviceContext;

        public DeviceContext(ID3D11DeviceContext* device)
        {
            _deviceContext = (IntPtr)device == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(device))
                : device;
        }

        public void CopyResource(Texture2D destination, Texture2D source)
            => _deviceContext->CopyResource(destination, source);

        public void CopySubResourceRegion(
            Texture2D dstResource,
            uint dstX,
            uint dstY,
            Texture2D sourceResource,
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

        public void OMSetBlendState(float[] blendFactor)
        {
            if(blendFactor.Length != 4)
                throw new ArgumentException("Blend factor must be an array with 4 length", nameof(blendFactor));

            fixed (float* refBlendFactor = &blendFactor[0])
                _deviceContext->OMSetBlendState(null, refBlendFactor, 0xFFFFFFFF);
        }

        public void OMSetRenderTargets(RenderTargetView renderTargetView)
        {
            var rtvHandle = (ID3D11RenderTargetView*)renderTargetView;
            _deviceContext->OMSetRenderTargets(1, ref rtvHandle, null);
        }

        public void VSSetShader(VertexShader shader)
        {
            _deviceContext->VSSetShader((ID3D11VertexShader*)shader, null, 0);
        }

        public void PSSetShader(PixelShader shader)
        {
            _deviceContext->PSSetShader((ID3D11PixelShader*)shader, null, 0);
        }

        public void PSSetShaderResources(ShaderResourceView sharedResourceView)
        {
            var srvHandle = (ID3D11ShaderResourceView*)sharedResourceView;
            _deviceContext->PSSetShaderResources(0,1, ref srvHandle);
        }

        public void PSSetSamplers(SamplerState samplerState)
        {
            var samplerHandle = (ID3D11SamplerState*)samplerState;
            _deviceContext->PSSetSamplers(0,1, ref samplerHandle);
        }

        public void IASetPrimitiveTopology(D3DPrimitiveTopology topology)
        {
            _deviceContext->IASetPrimitiveTopology(topology);
        }

        public void IASetVertexBuffers(Buffer buffer, uint strides, uint offsets)
        {
            var bufferHandler = (ID3D11Buffer*)buffer;
            _deviceContext->IASetVertexBuffers(0,1, ref bufferHandler, ref strides, ref offsets);
        }

        public void RSSetViewports(uint viewportNumber, Viewport viewport)
        {
            _deviceContext->RSSetViewports(viewportNumber, ref viewport);
        }

        public void Draw(uint vertexCount, uint vertexStartLocation)
        {
            _deviceContext->Draw(vertexCount, vertexStartLocation);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_deviceContext != null)
                _deviceContext->Release();
            _deviceContext = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~DeviceContext()
        {
            ReleaseUnmanagedResources();
        }
    }
}
