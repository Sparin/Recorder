using Silk.NET.DXGI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Message = Silk.NET.Direct3D11.Message;
using Recorder.Direct3D11.Interop.Direct3D;
using Buffer = Recorder.Direct3D11.Interop.Direct3D.Buffer;
using Recorder.Direct3D11.Interop.DXGI;

namespace Recorder.Direct3D11.Interop.Direct3D11
{
    public sealed unsafe class Device : IDisposable
    {
        private ID3D11Device* _device;

        public Device(ID3D11Device* device)
        {
            _device = (IntPtr)device == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(device))
                : device;
        }

        public void PrintMessage()
        {
            var riid = ID3D11InfoQueue.Guid;
            ID3D11InfoQueue* infoQueue;
            _device->QueryInterface(ref riid, (void**)&infoQueue);

            var messages = infoQueue->GetNumStoredMessagesAllowedByRetrievalFilter();
            for (var i = 0ul; i < messages; i++)
            {
                nuint size = 0;
                infoQueue->GetMessageA(i, null, ref size);
                Message* message = (Message*)Marshal.AllocHGlobal((int)size);

                infoQueue->GetMessageA(i, message, ref size);
                var s = Marshal.PtrToStringAnsi((IntPtr)message->PDescription, (int)message->DescriptionByteLength);
                Console.WriteLine($"[{message->Severity}] [{message->Category}] {s}");
            }
            infoQueue->Release();
        }

        public Texture2D CreateTexture2D(Texture2DDesc description)
        {
            ID3D11Texture2D* texture;
            var hresult = _device->CreateTexture2D(ref description, (SubresourceData*)IntPtr.Zero, &texture);
            //PrintMessage();
            SilkMarshal.ThrowHResult(hresult);
            return new Texture2D(texture);
        }

        public Texture2D OpenSharedResource(IntPtr handle)
        {
            var riid = ID3D11Texture2D.Guid;
            ID3D11Texture2D* texture;
            var hresult = _device->OpenSharedResource((void*)handle, &riid, (void**)&texture);
            SilkMarshal.ThrowHResult(hresult);
            return new Texture2D(texture);
        }

        public RenderTargetView CreateRenderTargetView(Texture2D texture)
        {
            ID3D11RenderTargetView* renderTargetView;
            var hresult = _device->CreateRenderTargetView((ID3D11Resource*)texture, null, &renderTargetView);
            SilkMarshal.ThrowHResult(hresult);
            return new RenderTargetView(renderTargetView);
        }

        public ShaderResourceView CreateShaderResourceView(Texture2D resource, ShaderResourceViewDesc description)
        {
            ID3D11ShaderResourceView* shaderResourceView;
            var hresult = _device->CreateShaderResourceView((ID3D11Resource*)resource, ref description, &shaderResourceView);
            SilkMarshal.ThrowHResult(hresult);
            return new ShaderResourceView(shaderResourceView);
        }

        public Buffer CreateBuffer(BufferDesc description, SubresourceData initialData)
        {
            ID3D11Buffer* buffer;
            var hresult = _device->CreateBuffer(ref description, ref initialData, &buffer);
            SilkMarshal.ThrowHResult(hresult);
            return new Buffer(buffer);
        }

        public static implicit operator ID3D11Device*(Device device) => device._device;

        public static implicit operator IUnknown*(Device device) => (IUnknown*)device._device;

        private void ReleaseUnmanagedResources()
        {
            if (_device != null)
                _device->Release();
            _device = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Device()
        {
            ReleaseUnmanagedResources();
        }
    }
}
