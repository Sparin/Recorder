using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Recorder.Direct3D11.Interop.DXGI
{
    public sealed unsafe class Adapter1 : IDisposable
    {
        private IDXGIAdapter1* _adapter;

        private AdapterDesc _description;
        public AdapterDesc Description => _description;

        internal Adapter1(IDXGIAdapter1* adapter)
        {
            _adapter = (IntPtr)adapter == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(adapter))
                : adapter;

            UpdateDescription();
        }

        private void UpdateDescription()
        {
            SilkMarshal.ThrowHResult(_adapter->GetDesc(ref _description));
        }

        public List<Output> GetOutputs()
        {
            var result = new List<Output>();

            var hResult = 0;
            for (var i = 0u; hResult != Constants.DXGI_ERROR_NOT_FOUND; i++)
            {
                IDXGIOutput* output;
                hResult = _adapter->EnumOutputs(i, &output);

                if (hResult == Constants.DXGI_ERROR_NOT_FOUND)
                    continue;

                if (hResult == 0)
                    result.Add(new Output(output));
                else
                    SilkMarshal.ThrowHResult(hResult);
            }

            return result;
        }

        public void PrintDiagnosticInformation(TextWriter writer)
        {
            var outputs = GetOutputs();
            fixed (AdapterDesc* descPtr = &_description)
            {
                writer.WriteLine(Marshal.PtrToStringAnsi((IntPtr)descPtr->Description, 128));
            }

            writer.WriteLine("{0,-30}: 0x{1:x8}", "Vendor Id", Description.VendorId);
            writer.WriteLine("{0,-30}: 0x{1:x8}", "Device Id", Description.DeviceId);
            writer.WriteLine("{0,-30}: {1:F} MB", "Dedicated Video Memory", (decimal)Description.DedicatedVideoMemory / 1024 / 1024);
            writer.WriteLine("{0,-30}: {1:F} MB", "Dedicated System Memory", (decimal)Description.DedicatedSystemMemory / 1024 / 1024);
            writer.WriteLine("{0,-30}: {1:F} MB", "Shared System Memory", (decimal)Description.SharedSystemMemory / 1024 / 1024);
            writer.WriteLine();

            if (!outputs.Any())
                return;
            writer.WriteLine("Outputs:");
            foreach (var output in outputs)
                output.PrintDiagnosticInformation(writer);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_adapter != null)
                _adapter->Release();
            _adapter = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Adapter1()
        {
            ReleaseUnmanagedResources();
        }
    }
}
