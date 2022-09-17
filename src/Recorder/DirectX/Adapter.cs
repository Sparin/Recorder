using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Recorder.DirectX
{
    public sealed unsafe class Adapter : IDisposable
    {
        private readonly IDXGIAdapter1* _adapter;

        public IEnumerable<Output> Outputs { get; }

        private AdapterDesc _description;
        public AdapterDesc Description => _description;

        internal Adapter(IDXGIAdapter1* adapter)
        {
            _adapter = (IntPtr)adapter == IntPtr.Zero
                ? throw new ArgumentNullException(nameof(adapter))
                : adapter;

            Outputs = GetOutputs();
            UpdateDescription();
        }

        private void UpdateDescription()
        {
            SilkMarshal.ThrowHResult(_adapter->GetDesc(ref _description));
        }

        private List<Output> GetOutputs()
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

            if (!Outputs.Any())
                return;
            writer.WriteLine("Outputs:");
            foreach (var output in Outputs)
                output.PrintDiagnosticInformation(writer);
        }

        private void ReleaseUnmanagedResources()
        {
            _adapter->Release();
        }

        public void Dispose()
        {
            foreach (var output in Outputs)
                output.Dispose();
            
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Adapter()
        {
            ReleaseUnmanagedResources();
        }
    }
}
