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
    public sealed unsafe class OutputDuplication : IDisposable
    {
        private IDXGIOutputDuplication* _outputDuplication;
        private D3DTexture2D? _lastFrame;
        private bool frameAcquired = false;

        public Output Parent { get; }

        public OutputDuplication(Output parent, IDXGIOutputDuplication* outputDuplication)
        {
            Parent = parent;
            _outputDuplication = (IntPtr)outputDuplication == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(outputDuplication))
                : outputDuplication;
        }

        private void ReleaseUnmanagedResources()
        {
            if ((ID3D11Texture2D*)_lastFrame != null)
                _lastFrame?.Dispose();

            _outputDuplication->Release();
            _outputDuplication = null;
        }

        public D3DTexture2D AcquireNextFrame(uint timeoutInMilliseconds)
        {
            IDXGIResource* resource;
            OutduplFrameInfo frameInfo;

            // Release previous frame
            if (_lastFrame != null)
            {
                _lastFrame.Dispose();
                _lastFrame = null;
            }

            if (frameAcquired)
                _outputDuplication->ReleaseFrame();

            SilkMarshal.ThrowHResult(_outputDuplication->AcquireNextFrame(timeoutInMilliseconds, &frameInfo, &resource));
            frameAcquired = true;
            
            var riid = ID3D11Texture2D.Guid;
            ID3D11Texture2D* frame;
            SilkMarshal.ThrowHResult(resource->QueryInterface(ref riid, (void**)&frame));
            resource->Release();

            _lastFrame = new D3DTexture2D(frame);
            return _lastFrame;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~OutputDuplication()
        {
            ReleaseUnmanagedResources();
        }
    }
}
