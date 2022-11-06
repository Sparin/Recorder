using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder.Direct3D11.Interop.Direct3D
{
    public sealed unsafe class Buffer : IDisposable
    {
        private ID3D11Buffer* _buffer;

        public Buffer(ID3D11Buffer* buffer)
        {
            _buffer = (IntPtr)buffer == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(buffer))
                : buffer;
        }

        public static implicit operator ID3D11Buffer*(Buffer buffer) => buffer._buffer;

        private void ReleaseUnmanagedResources()
        {
            if (_buffer != null)
                _buffer->Release();
            _buffer = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Buffer()
        {
            ReleaseUnmanagedResources();
        }
    }
}
