using Recorder.Direct3D11.Core;
using Recorder.Direct3D11.Interop.Direct3D11;
using Recorder.Direct3D11.Interop.DXGI;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;

namespace Testify
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var factory = new Factory1();
            var adapters = factory.GetAdapters();

            var (device, context) = CreateDevice();

            for (var i = 0; i < adapters.Count; i++)
            {
                adapters[i].PrintDiagnosticInformation(Console.Out);

                var outputs = adapters[i].GetOutputs();

                if (outputs.Any())
                {
                    var output = outputs.First(x => x.Description.DesktopCoordinates.Origin == Vector2D<int>.Zero);

                    var dd = new DisplayDuplication(device, output);
                    while (true)
                    {
                        var data = dd.AcquireNextFrame(500, out var isTimeoutExceeded);
                        if (isTimeoutExceeded)
                        {
                            Console.WriteLine("Timeout exceeded");
                        }
                        else
                        {
                            Console.WriteLine($"Frame acquired {data.DirtyRectanglesCount}, {data.MoveRectanglesCount}");
                            dd.ReleaseFrame();
                        }

                        await Task.Delay(1000 / 30);
                    }
                }

            }


        }

        private unsafe static (Device, DeviceContext) CreateDevice()
        {
            var _d3d = D3D11.GetApi();
            var riid = ID3D11Device.Guid;
            ID3D11Device* device;
            ID3D11DeviceContext* deviceContext;
            var supportedFeatureLevel = D3DFeatureLevel.Level91;

            var features = new[]
            {
                D3DFeatureLevel.Level110,
                D3DFeatureLevel.Level101,
                D3DFeatureLevel.Level100,
                D3DFeatureLevel.Level91
            };
            fixed (D3DFeatureLevel* pFeatures = &features[0])
            {
                SilkMarshal.ThrowHResult(
                    _d3d.CreateDevice(
                        pAdapter: (IDXGIAdapter*)IntPtr.Zero,
                        DriverType: D3DDriverType.Hardware,
                        Software: 0,
                        Flags: 0,// (uint)CreateDeviceFlag.Debug,
                        pFeatureLevels: pFeatures,
                        FeatureLevels: (uint)features.Length,
                        SDKVersion: D3D11.SdkVersion,
                        ppDevice: &device,
                        pFeatureLevel: &supportedFeatureLevel,
                        ppImmediateContext: &deviceContext));
            }

            return (new Device(device), new DeviceContext(deviceContext));
        }
    }
}