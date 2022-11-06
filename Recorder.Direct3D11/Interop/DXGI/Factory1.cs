using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Recorder.Direct3D11.Interop.DXGI
{
    public sealed unsafe class Factory1 : IDisposable
    {
        private readonly ILogger _logger;
        private static readonly Silk.NET.DXGI.DXGI Dxgi = Silk.NET.DXGI.DXGI.GetApi();
        private IDXGIFactory1* _factory;


        public Factory1(ILogger? logger = null) : this(CreateFactory(), logger)
        {
        }

        public Factory1(IDXGIFactory1* factory, ILogger? logger = null)
        {
            _factory = (IntPtr)factory == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(factory))
                : factory;
            _logger = logger ?? NullLogger.Instance;

        }

        public List<Adapter1> GetAdapters()
        {
            var result = new List<Adapter1>();

            var hResult = 0;
            for (var i = 0u; hResult != Constants.DXGI_ERROR_NOT_FOUND; i++)
            {
                IDXGIAdapter1* adapter;
                hResult = _factory->EnumAdapters1(i, &adapter);

                if (hResult == Constants.DXGI_ERROR_NOT_FOUND)
                    continue;

                if (hResult == 0)
                    result.Add(new Adapter1(adapter));
                else
                    SilkMarshal.ThrowHResult(hResult);
            }

            return result;
        }

        private static IDXGIFactory1* CreateFactory()
        {
            var riid = IDXGIFactory1.Guid;
            IDXGIFactory1* factory;
            SilkMarshal.ThrowHResult(Dxgi.CreateDXGIFactory1(ref riid, (void**)&factory));

            return factory;
        }

        private void ReleaseUnmanagedResources()
        {
            if (_factory != null)
                _factory->Release();
            _factory = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Factory1()
        {
            ReleaseUnmanagedResources();
        }
    }
}
