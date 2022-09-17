using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Recorder.DirectX
{
    public sealed unsafe class Output : IDisposable
    {
        private readonly IDXGIOutput* _output;

        private OutputDesc _description;
        public OutputDesc Description => _description;

        public Output(IDXGIOutput* output)
        {
            _output = (IntPtr)output == IntPtr.Zero 
                ? throw new ArgumentNullException(nameof(output))
                : output;

            UpdateDescription();
        }

        // TODO: Duplicate output supports duplication only on D3D device which has been created using adapter of output
        public OutputDuplication DuplicateOutput(D3DDevice device)
        {
            var riid = IDXGIOutput1.Guid;
            IDXGIOutput1* output1;
            IDXGIOutputDuplication* duplicationOutput;

            SilkMarshal.ThrowHResult(_output->QueryInterface(ref riid, (void**)&output1));
            SilkMarshal.ThrowHResult(output1->DuplicateOutput(device, &duplicationOutput));
            output1->Release();

            return new OutputDuplication(this, duplicationOutput);
        }

        public void PrintDiagnosticInformation(TextWriter writer)
        {
            fixed (OutputDesc* descPtr = &_description)
            {
                writer.WriteLine("\t{0,-30}: {1}", "Device Name",
                    Marshal.PtrToStringAnsi((IntPtr)descPtr->DeviceName, 32));
            }

            writer.WriteLine("\t{0,-30}: {1}", "Attached to the desktop",
                Convert.ToBoolean(Description.AttachedToDesktop));
            writer.WriteLine("\t{0,-30}: {1}", "Coordinates",
                $"pos ({Description.DesktopCoordinates.Origin.X}, {Description.DesktopCoordinates.Origin.Y}) " +
                $"size ({Description.DesktopCoordinates.Size.X - Description.DesktopCoordinates.Origin.X}, {Description.DesktopCoordinates.Size.Y - Description.DesktopCoordinates.Origin.Y})");
            writer.WriteLine("\t{0,-30}: {1}", "Rotation", Description.Rotation);
            writer.WriteLine();
        }
        private void UpdateDescription()
        {
            SilkMarshal.ThrowHResult(_output->GetDesc(ref _description));
        }

        private void ReleaseUnmanagedResources()
        {
            _output->Release();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Output()
        {
            ReleaseUnmanagedResources();
        }
    }
}
