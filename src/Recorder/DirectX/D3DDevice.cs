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

namespace Recorder.DirectX
{
    public sealed unsafe class D3DDevice : IDisposable
    {
        private readonly ID3D11Device* _device;

        public D3DDevice(ID3D11Device* device)
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

        public D3DTexture2D CreateTexture2D(Texture2DDesc description)
        {
            ID3D11Texture2D* texture;
            var hresult = _device->CreateTexture2D(ref description, (SubresourceData*)IntPtr.Zero, &texture);
            //PrintMessage();
            SilkMarshal.ThrowHResult(hresult);
            return new D3DTexture2D(texture);
        }

        public static implicit operator ID3D11Device*(D3DDevice device) => device._device;

        public static implicit operator IUnknown*(D3DDevice device)=> (IUnknown*)device._device;

        private void ReleaseUnmanagedResources()
        {
            _device->Release();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~D3DDevice()
        {
            ReleaseUnmanagedResources();
        }
    }
}
