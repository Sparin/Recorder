using Silk.NET.Direct3D11;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder.Direct3D11.Interop.Direct3D
{
    public sealed unsafe class VertexShader : IDisposable
    {
        private ID3D11VertexShader* _shader;

        public VertexShader(ID3D11VertexShader* buffer)
        {
            _shader = (IntPtr)buffer == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(buffer))
                : buffer;
        }

        public static implicit operator ID3D11VertexShader*(VertexShader shader) => shader._shader;

        private void ReleaseUnmanagedResources()
        {
            if (_shader != null)
                _shader->Release();
            _shader = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~VertexShader()
        {
            ReleaseUnmanagedResources();
        }
    }
}
