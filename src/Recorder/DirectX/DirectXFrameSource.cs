using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Recorder.DirectX
{
    public sealed unsafe class DirectXFrameSource : IFrameSource
    {
        private readonly DXGI _dxgi;
        private readonly D3D11 _d3d;
        private readonly ILogger<DirectXFrameSource> _logger;

        public Factory Factory { get; }
        public D3DDevice Device { get; }
        public D3DDeviceContext DeviceContext { get; }
        public D3DTexture2D SharedBuffer { get; }

        private byte[] _buffer;
        private GCHandle _bufferHandle;
        private Rectangle _dimensions;

        private List<OutputDuplication> _outputDuplications;

        public int Height => _dimensions.Height;
        public int Width => _dimensions.Width;

        public DirectXFrameSource(ILogger<DirectXFrameSource>? logger = null)
        {
            _dxgi = DXGI.GetApi();
            _d3d = D3D11.GetApi();
            _logger = logger ?? NullLogger<DirectXFrameSource>.Instance;

            Factory = CreateFactory();
            (Device, DeviceContext) = CreateDevice();
            _dimensions = EstimateDesktopDimensions();
            SharedBuffer = CreateSharedBuffer(_dimensions);
            _buffer = new byte[_dimensions.Width * Height * 4];
            _outputDuplications = CreateDuplicationOutputs();
        }

        public ReadOnlyMemory<byte> AcquireNextFrame()
        {
            foreach (var outputDuplication in _outputDuplications)
            {
                using var frame = outputDuplication.AcquireNextFrame(500000);
                var s = outputDuplication.Parent.Description.DesktopCoordinates;

                D3DTexture2D? fr = null;
                if (outputDuplication.Parent.Description.Rotation != ModeRotation.Identity)
                {
                    var frameDesc = frame.Description;
                    frameDesc.SampleDesc = new SampleDesc(1, 0);
                    frameDesc.CPUAccessFlags = (uint)CpuAccessFlag.Read;
                    frameDesc.BindFlags = 0;
                    frameDesc.MipLevels = 1;
                    frameDesc.MiscFlags = 0;
                    frameDesc.ArraySize = 1;
                    frameDesc.Usage = Usage.Staging;
                    fr = Device.CreateTexture2D(frameDesc);
                    DeviceContext.CopyResource(fr, frame);
                }

                switch (outputDuplication.Parent.Description.Rotation)
                {
                    case ModeRotation.None:
                    case ModeRotation.Identity:
                    default:
                        DeviceContext.CopySubResourceRegion(SharedBuffer, (uint)(s.Origin.X - _dimensions.X),
                            (uint)(s.Origin.Y - _dimensions.Y),
                            frame, new Box(0, 0, 0, (uint)(s.Size.X - s.Origin.X), (uint)(s.Size.Y - s.Origin.Y), 1));
                        break;

                    case ModeRotation.Rotate180:
                        using (var sharedSurface = SharedBuffer.GetSurface())
                        using (var frameSurface = fr.GetSurface())
                        {
                            var sharedMap = sharedSurface.Map(DXGI.MapWrite);
                            var frameMap = frameSurface.Map(DXGI.MapRead);
                            Parallel.For(0, s.Size.Y - s.Origin.Y, y =>
                            {
                                Parallel.For(0, s.Size.X - s.Origin.X, x =>
                                {
                                    var srcX = frame.Description.Width - x;
                                    var srcY = frame.Description.Height - y;
                                    var dstX = s.Origin.X - _dimensions.X + x;
                                    var dstY = s.Origin.Y - _dimensions.Y + y;

                                    var srcRowPtr = frameMap.Pitch * (srcY - 1);
                                    var dstRowPtr = sharedMap.Pitch * dstY;

                                    for (var i = 0; i < 4; i++)
                                        sharedMap.PBits[dstRowPtr + dstX * 4 + i] =
                                            frameMap.PBits[srcRowPtr + (srcX - 1) * 4 + i];
                                });
                            });
                            frameSurface.Unmap();
                            sharedSurface.Unmap();
                        }

                        break;

                    case ModeRotation.Rotate90:
                    case ModeRotation.Rotate270:
                        using (var sharedSurface = SharedBuffer.GetSurface())
                        using (var frameSurface = fr.GetSurface())
                        {
                            var sharedMap = sharedSurface.Map(DXGI.MapWrite);
                            var frameMap = frameSurface.Map(DXGI.MapRead);

                            if (outputDuplication.Parent.Description.Rotation == ModeRotation.Rotate90)
                                for (var x = 0; x < (uint)(s.Size.X - s.Origin.X); x++)
                                for (var y = 0; y < (uint)(s.Size.Y - s.Origin.Y); y++)
                                {
                                    var srcX = y;
                                    var srcY = frame.Description.Height - x;

                                    var dstX = s.Origin.X - _dimensions.X + x;
                                    var dstY = s.Origin.Y - _dimensions.Y + y;

                                    var srcRowPtr = frameMap.Pitch * srcY;
                                    var dstRowPtr = sharedMap.Pitch * dstY;
                                    for (var i = 0; i < 4; i++)
                                        sharedMap.PBits[dstRowPtr + dstX * 4 + i] =
                                            frameMap.PBits[srcRowPtr + srcX * 4 + i];
                                }
                            else
                                for (var x = 0; x <= (uint)(s.Size.X - s.Origin.X); x++)
                                for (var y = 0; y < (uint)(s.Size.Y - s.Origin.Y); y++)
                                {
                                    var srcX = frame.Description.Width - y;
                                    var srcY = frame.Description.Height - x;

                                    var dstX = s.Origin.X - _dimensions.X + frame.Description.Height - x;
                                    var dstY = s.Origin.Y - _dimensions.Y + y;

                                    var srcRowPtr = frameMap.Pitch * srcY;
                                    var dstRowPtr = sharedMap.Pitch * dstY;
                                    for (var i = 0; i < 4; i++)
                                        sharedMap.PBits[dstRowPtr + dstX * 4 + i] =
                                            frameMap.PBits[srcRowPtr + (srcX - 1) * 4 + i];
                                }

                            frameSurface.Unmap();
                            sharedSurface.Unmap();
                        }

                        break;
                }

                fr?.Dispose();
            }

            using var surface = SharedBuffer.GetSurface();
            var map = surface.Map(DXGI.MapRead);

            // 2D texture aligned properly
            if (map.Pitch == _dimensions.Width * 4)
            {
                Marshal.Copy((IntPtr)map.PBits, _buffer, 0, _buffer.Length);
            }
            // 2D texture aligned with overlap
            else
            {
                var rowLength = _dimensions.Width * 4;
                
                // I don't know how make a partial copy using Marshal.Copy
                for (var row = 0; row < _dimensions.Height; row++)
                    for (int i = 0; i < rowLength; i++)
                        _buffer[row * rowLength + i] = map.PBits[row * map.Pitch + i];

            }
            surface.Unmap();

            return _buffer.AsMemory();
        }


        private Factory CreateFactory()
        {
            var riid = IDXGIFactory1.Guid;
            IDXGIFactory1* factory;
            SilkMarshal.ThrowHResult(_dxgi.CreateDXGIFactory1(ref riid, (void**)&factory));
            return new Factory(factory);
        }

        private (D3DDevice, D3DDeviceContext) CreateDevice()
        {
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

            _logger.LogInformation("D3D Device created with supported level {level}", supportedFeatureLevel);

            return (new D3DDevice(device), new D3DDeviceContext(deviceContext));
        }

        private Rectangle EstimateDesktopDimensions()
        {
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            var outputs = Factory.Adapters.SelectMany(f => f.Outputs);

            if (!outputs.Any())
                throw new Exception("Can't estimate desktop dimensions due to no available outputs");

            foreach (var output in outputs)
            {
                var desktopCoordinates = output.Description.DesktopCoordinates;
                left = Math.Min(desktopCoordinates.Origin.X, left);
                top = Math.Min(desktopCoordinates.Origin.Y, top);
                right = Math.Max(desktopCoordinates.Size.X, right);
                bottom = Math.Max(desktopCoordinates.Size.Y, bottom);
            }

            return new Rectangle(left, top, right - left, bottom - top);
        }

        private D3DTexture2D CreateSharedBuffer(Rectangle dimensions)
        {
            var desciption = new Texture2DDesc()
            {
                Width = (uint)dimensions.Width,
                Height = (uint)dimensions.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.FormatB8G8R8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Staging,
                BindFlags = 0,//(uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
                CPUAccessFlags = (uint)(CpuAccessFlag.Read | CpuAccessFlag.Write),
                MiscFlags = 0//(uint)ResourceMiscFlag.SharedKeyedmutex 
            };

            return Device.CreateTexture2D(desciption);
        }

        private List<OutputDuplication> CreateDuplicationOutputs() =>
            Factory.Adapters.SelectMany(f => f.Outputs)
                .Select(output => output.DuplicateOutput(Device))
                .ToList();

        public void PrintDiagnosticInformation(TextWriter writer)
        {
            if (!Factory.Adapters.Any())
                return;

            writer.WriteLine("Adapters");
            foreach (var adapter in Factory.Adapters)
                adapter.PrintDiagnosticInformation(writer);
            writer.WriteLine();
        }
    }
}
