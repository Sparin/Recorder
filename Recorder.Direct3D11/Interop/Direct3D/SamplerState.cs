using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder.Direct3D11.Interop.Direct3D
{
    public sealed unsafe class SamplerState : IDisposable
    {
        private ID3D11SamplerState* _samplerState;

        public SamplerState(ID3D11SamplerState* buffer)
        {
            _samplerState = (IntPtr)buffer == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(buffer))
                : buffer;
        }

        public static implicit operator ID3D11SamplerState*(SamplerState samplerState) => samplerState._samplerState;

        private void ReleaseUnmanagedResources()
        {
            if (_samplerState != null)
                _samplerState->Release();
            _samplerState = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~SamplerState()
        {
            ReleaseUnmanagedResources();
        }
    }
}
