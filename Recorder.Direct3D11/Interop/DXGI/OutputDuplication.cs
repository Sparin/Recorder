using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Recorder.Direct3D11.Interop.Direct3D11;
using Silk.NET.Maths;

namespace Recorder.Direct3D11.Interop.DXGI
{
    public sealed unsafe class OutputDuplication : IDisposable
    {
        private IDXGIOutputDuplication* _outputDuplication;
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
            if (_outputDuplication != null)
                _outputDuplication->Release();
            _outputDuplication = null;
        }

        public (OutduplFrameInfo, Resource?) AcquireNextFrame(uint timeoutInMilliseconds, out bool isTimeoutExceeded)
        {
            IDXGIResource* resourceRef;
            OutduplFrameInfo frameInfo;
            isTimeoutExceeded = false;

            var hresult = _outputDuplication->AcquireNextFrame(timeoutInMilliseconds, &frameInfo, &resourceRef);
            if (hresult == Constants.DXGI_ERROR_WAIT_TIMEOUT)
            {
                isTimeoutExceeded = true;
                return (default, null);
            }
            
            SilkMarshal.ThrowHResult(hresult);
            return (frameInfo, new Resource(resourceRef));
        }

        public void GetFrameMoveRects(uint bufferSize, IntPtr bufferHandle, out uint requiredBufferSize)
        {
            requiredBufferSize = 0;
            var hresult = _outputDuplication->GetFrameMoveRects(bufferSize, (OutduplMoveRect*)bufferHandle, ref requiredBufferSize);
            SilkMarshal.ThrowHResult(hresult);
        }

        public void GetFrameDirtyRects(uint bufferSize, IntPtr bufferHandle, out uint requiredBufferSize)
        {
            requiredBufferSize = 0;
            var hresult = _outputDuplication->GetFrameDirtyRects(bufferSize, (Rectangle<int>*)bufferHandle, ref requiredBufferSize);
            SilkMarshal.ThrowHResult(hresult);
        }

        public void ReleaseFrame()
        {
            var hresult = _outputDuplication->ReleaseFrame();
            SilkMarshal.ThrowHResult(hresult);
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
