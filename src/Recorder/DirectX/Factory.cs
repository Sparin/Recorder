using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Recorder.DirectX
{
    public sealed unsafe class Factory : IDisposable
    {
        private readonly IDXGIFactory1* _factory;

        public IEnumerable<Adapter> Adapters { get; }

        public Factory(IDXGIFactory1* factory)
        {
            _factory = (IntPtr)factory == IntPtr.Zero 
                ? throw new ArgumentNullException(nameof(factory))
                : factory;

            Adapters = GetAdapters();
        }

        private List<Adapter> GetAdapters()
        {
            var result = new List<Adapter>();

            var hResult = 0;
            for (var i = 0u; hResult != Constants.DXGI_ERROR_NOT_FOUND; i++)
            {
                IDXGIAdapter1* adapter;
                hResult = _factory->EnumAdapters1(i, &adapter);

                if (hResult == Constants.DXGI_ERROR_NOT_FOUND)
                    continue;

                if (hResult == 0)
                    result.Add(new Adapter(adapter));
                else
                    SilkMarshal.ThrowHResult(hResult);
            }

            return result;
        }

        private void ReleaseUnmanagedResources()
        {
            _factory->Release();
        }

        public void Dispose()
        {
            foreach (var adapter in Adapters)
                adapter.Dispose();

            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Factory()
        {
            ReleaseUnmanagedResources();
        }
    }
}
