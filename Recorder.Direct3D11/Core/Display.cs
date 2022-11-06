using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Recorder.Direct3D11.Interop.Direct3D;
using Recorder.Direct3D11.Interop.Direct3D11;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Buffer = Recorder.Direct3D11.Interop.Direct3D.Buffer;


namespace Recorder.Direct3D11.Core
{
    public class Display
    {
        private DisplayDuplication _displayDuplication;
        private CancellationTokenSource _stoppingCts;
        private Task _executeTask;

        private Texture2D? _moveSurface;
        private RenderTargetView? _renderTargetView;

        private byte[] _dirtyVertexBuffer = Array.Empty<byte>();

        private readonly uint _outputIdentifier;
        private readonly int _offsetX;
        private readonly int _offsetY;
        private readonly Texture2D _sharedBuffer;
        private readonly PointerInfo _pointerInfo;

        private readonly Device _device;
        private readonly DeviceContext _context;
        private readonly VertexShader _vertexShader;
        private readonly PixelShader _pixelShader;
        private readonly InputLayout _inputLayout;
        private readonly SamplerState _linearSampler;

        private readonly Texture2D _sharedSurface;

        public Display(
            uint outputIdentifier,
            int offsetX,
            int offsetY,
            IntPtr sharedBufferHandle,
            PointerInfo pointerInfo,
            DisplayDirectXArgs d3dResources)
        {
            _outputIdentifier = outputIdentifier;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _sharedBuffer = d3dResources.Device.OpenSharedResource(sharedBufferHandle);
            _pointerInfo = pointerInfo;

            _device = d3dResources.Device;
            _context = d3dResources.Context;
            _vertexShader = d3dResources.VertexShader;
            _pixelShader = d3dResources.PixelShader;
            _inputLayout = d3dResources.InputLayout;
            _linearSampler = d3dResources.LinearSampler;
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            // Create linked token to allow cancelling executing task from provided token
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            //_executeTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (_executeTask.IsCompleted)
            {
                return _executeTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executeTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executeTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
            }

        }


        // TODO: Implement
        public Task UpdateSharedBuffer(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var frameData = _displayDuplication.AcquireNextFrame(500, out var isTimeoutExceeded);

                try
                {
                    if (isTimeoutExceeded)
                        continue;

                    // Get mutex on shared resource
                    //KeyMutex->AcquireSync(0, 1000);

                    // Get Pointer
                    // DuplMgr.GetMouse(TData->PtrInfo, &(CurrentData.FrameInfo), TData->OffsetX, TData->OffsetY);

                    UpdateSharedSurface(frameData, _sharedSurface, _offsetX, _offsetY, _displayDuplication.OutputDescription);

                    //Release Mutex

                }
                finally
                {
                    _displayDuplication.ReleaseFrame();
                }
            }

            return Task.CompletedTask;
        }

        public unsafe void UpdateSharedSurface(
            FrameData frameData,
            Texture2D sharedSurface,
            int offsetX,
            int offsetY,
            OutputDesc desktopDescription)
        {
            if (frameData.Metadata.IsEmpty)
                return;

            var frameDescription = frameData.Frame.Description;

            var moveRectangles = MemoryMarshal.Cast<byte, OutduplMoveRect>(frameData.Metadata.Span.Slice(0,(int)(frameData.MoveRectanglesCount * sizeof(OutduplMoveRect))));
            if (frameData.MoveRectanglesCount > 0)
                CopyMove(sharedSurface, moveRectangles, offsetX, offsetY, desktopDescription, frameDescription.Width, frameDescription.Height);

            var dirtyRectangles = MemoryMarshal.Cast<byte, Rectangle<int>>(frameData.Metadata.Span.Slice((int)(frameData.MoveRectanglesCount * sizeof(OutduplMoveRect))));
            if (frameData.DirtyRectanglesCount > 0)
                CopyDirty(frameData.Frame, sharedSurface, dirtyRectangles, offsetX, offsetY, desktopDescription);
        }



        private void CopyMove(Texture2D sharedSurface, Span<OutduplMoveRect> moveRectangles, int offsetX, int offsetY, OutputDesc desktopDescription, uint textureWidth, uint textureHeight)
        {
            if (_moveSurface == null)
            {
                var moveDescription = sharedSurface.Description;
                moveDescription.Width = (uint)(desktopDescription.DesktopCoordinates.Size.X - desktopDescription.DesktopCoordinates.Origin.X);
                moveDescription.Width = (uint)(desktopDescription.DesktopCoordinates.Size.Y - desktopDescription.DesktopCoordinates.Origin.Y);
                moveDescription.BindFlags = (uint)BindFlag.RenderTarget;
                moveDescription.MiscFlags = 0;
                _moveSurface = _device.CreateTexture2D(moveDescription);
            }

            var dc = desktopDescription.DesktopCoordinates;

            for (var i = 0; i < moveRectangles.Length; i++)
            {
                var (srcRectangle, dstRectangle) = 
                    GetMoveRectanglesCoordinates(desktopDescription, moveRectangles[i], textureWidth, textureHeight);

                var box = new Box();
                box.Left = (uint)(srcRectangle.Origin.X + dc.Origin.X - offsetX);
                box.Top = (uint)(srcRectangle.Origin.Y + dc.Origin.Y - offsetY);
                box.Front = 0;
                box.Right = (uint)(srcRectangle.Size.X + dc.Origin.X - offsetX);
                box.Bottom = (uint)(srcRectangle.Size.Y + dc.Origin.Y - offsetY);
                box.Back = 1;
                _context.CopySubResourceRegion(_moveSurface, 0, (uint)srcRectangle.Origin.X, sharedSurface, box);

                box.Left = (uint)(srcRectangle.Origin.X);
                box.Top = (uint)(srcRectangle.Origin.Y);
                box.Front = 0;
                box.Right = (uint)(srcRectangle.Size.X);
                box.Bottom = (uint)(srcRectangle.Size.Y);
                box.Back = 1;
                _context.CopySubResourceRegion(
                    sharedSurface, 
                    (uint)(dstRectangle.Origin.X + dc.Origin.X - offsetX), 
                    (uint)(dstRectangle.Origin.Y + dc.Origin.Y - offsetY),
                    _moveSurface,
                    box);
            }
        }


        private (Rectangle<int>, Rectangle<int>) GetMoveRectanglesCoordinates(
            OutputDesc desktopDescription,
            OutduplMoveRect moveRectangle,
            uint textureWidth,
            uint textureHeight)
        {
            Rectangle<int> sourceRectangle = new(), destinationRectangle = new();
            switch (desktopDescription.Rotation)
            {
                case ModeRotation.Unspecified:
                case ModeRotation.Identity:
                    sourceRectangle.Origin.X = moveRectangle.SourcePoint.X;
                    sourceRectangle.Origin.Y = moveRectangle.SourcePoint.Y;
                    sourceRectangle.Size.X = moveRectangle.SourcePoint.X + moveRectangle.DestinationRect.Size.X - moveRectangle.DestinationRect.Origin.X;
                    sourceRectangle.Size.X = moveRectangle.SourcePoint.Y + moveRectangle.DestinationRect.Size.Y - moveRectangle.DestinationRect.Origin.Y;

                    destinationRectangle = sourceRectangle;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return (sourceRectangle, destinationRectangle);
        }

        private unsafe void CopyDirty(Texture2D sourceSurface, Texture2D sharedSurface, Span<Rectangle<int>> dirtyRectangles, int offsetX, int offsetY, OutputDesc desktopDescription)
        {
            var fullDesc = sharedSurface.Description;
            var thisDesc = sourceSurface.Description;

            _renderTargetView ??= _device.CreateRenderTargetView(sharedSurface);
            ShaderResourceViewDesc shaderDescription = new ShaderResourceViewDesc
            {
                Format = thisDesc.Format,
                ViewDimension = D3DSrvDimension.D3D11SrvDimensionTexture2D,
                Texture2D = new Tex2DSrv(thisDesc.MipLevels - 1, thisDesc.MipLevels)
            };
            using var shaderResource = _device.CreateShaderResourceView(sourceSurface, shaderDescription);
            _context.OMSetBlendState(new[] { 0f, 0f, 0f, 0f });
            _context.OMSetRenderTargets(_renderTargetView);
            _context.VSSetShader(_vertexShader);
            _context.PSSetShader(_pixelShader);
            _context.PSSetShaderResources(shaderResource);
            _context.PSSetSamplers(_linearSampler);
            _context.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);

            var minimumVertexBufferSize = sizeof(Vertex) * Common.NumberOfVertices * dirtyRectangles.Length;

            if (_dirtyVertexBuffer.Length < minimumVertexBufferSize)
                _dirtyVertexBuffer = new byte[minimumVertexBufferSize];

            var dirtyVertexes = MemoryMarshal.Cast<byte, Vertex>(_dirtyVertexBuffer.AsSpan(0, minimumVertexBufferSize));

            for (int i = 0; i < dirtyRectangles.Length; i++)
            {
                SetDirtyVert(dirtyVertexes, dirtyRectangles[i], offsetX, offsetY, desktopDescription,fullDesc, thisDesc);
                dirtyVertexes = dirtyVertexes.Slice(Common.NumberOfVertices);
            }

            var bufferDescription = new BufferDesc()
            {
                Usage = Usage.Default,
                ByteWidth = (uint)minimumVertexBufferSize,
                BindFlags = (uint)BindFlag.VertexBuffer,
                CPUAccessFlags = 0
            };
            Buffer vertBuffer;
            fixed (void* initialDataPointer = &_dirtyVertexBuffer[0])
            {
                var bufferInitialData = new SubresourceData()
                {
                    PSysMem = initialDataPointer
                };
                vertBuffer = _device.CreateBuffer(bufferDescription, bufferInitialData);
            }
            _context.IASetVertexBuffers(vertBuffer, (uint)sizeof(Vertex), 0);
            var viewport = new Viewport()
            {
                Width = fullDesc.Width,
                Height = fullDesc.Height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
                TopLeftX = 0.0f,
                TopLeftY = 0.0f
            };

            _context.RSSetViewports(1, viewport);
            _context.Draw(Common.NumberOfVertices, 0);

            vertBuffer.Dispose();
        }

        private void SetDirtyVert(Span<Vertex> vertices, Rectangle<int> dirtyRectangle, int offsetX,
            int offsetY, OutputDesc desktopDescription, Texture2DDesc fullDescription, Texture2DDesc thisDescription)
        {
            var centerX = fullDescription.Width / 2;
            var centerY = fullDescription.Height / 2;

            var width = desktopDescription.DesktopCoordinates.Size.X - desktopDescription.DesktopCoordinates.Origin.X;
            var height = desktopDescription.DesktopCoordinates.Size.Y - desktopDescription.DesktopCoordinates.Origin.Y;

            switch (desktopDescription.Rotation)
            {
                case ModeRotation.Identity:
                case ModeRotation.Unspecified:
                    vertices[0].TextureCoordinates = new Vector2D<float>(Convert.ToSingle(dirtyRectangle.Origin.X) / thisDescription.Width, Convert.ToSingle(dirtyRectangle.Size.Y) / thisDescription.Height);
                    vertices[1].TextureCoordinates = new Vector2D<float>(Convert.ToSingle(dirtyRectangle.Origin.X) / thisDescription.Width, Convert.ToSingle(dirtyRectangle.Origin.Y) / thisDescription.Height);
                    vertices[2].TextureCoordinates = new Vector2D<float>(Convert.ToSingle(dirtyRectangle.Size.X) / thisDescription.Width, Convert.ToSingle(dirtyRectangle.Size.Y) / thisDescription.Height);
                    vertices[5].TextureCoordinates = new Vector2D<float>(Convert.ToSingle(dirtyRectangle.Size.X) / thisDescription.Width, Convert.ToSingle(dirtyRectangle.Origin.Y) / thisDescription.Height);
                    break;

                default:
                    throw new NotImplementedException();
            }

            vertices[0].Position = new Vector3D<float>(
                Convert.ToSingle(dirtyRectangle.Origin.X + desktopDescription.DesktopCoordinates.Origin.X - offsetX - centerX) / centerX,
                Convert.ToSingle(-1 * (dirtyRectangle.Size.Y + desktopDescription.DesktopCoordinates.Origin.Y - offsetY - centerY))/ centerY,
                0.0f);

            vertices[1].Position = new Vector3D<float>(
                Convert.ToSingle(dirtyRectangle.Origin.X + desktopDescription.DesktopCoordinates.Origin.X - offsetX - centerX) / centerX,
                Convert.ToSingle(-1 * (dirtyRectangle.Origin.Y + desktopDescription.DesktopCoordinates.Origin.Y - offsetY - centerY)) / centerY,
                0.0f);

            vertices[2].Position = new Vector3D<float>(
                Convert.ToSingle(dirtyRectangle.Size.X + desktopDescription.DesktopCoordinates.Origin.X - offsetX - centerX) / centerX,
                Convert.ToSingle(-1 * (dirtyRectangle.Size.Y + desktopDescription.DesktopCoordinates.Origin.Y - offsetY - centerY)) / centerY,
                0.0f);

            vertices[3].Position = vertices[2].Position;
            vertices[4].Position = vertices[1].Position;

            vertices[5].Position = new Vector3D<float>(
                Convert.ToSingle(dirtyRectangle.Size.X + desktopDescription.DesktopCoordinates.Origin.X - offsetX - centerX) / centerX,
                Convert.ToSingle(-1 * (dirtyRectangle.Origin.Y + desktopDescription.DesktopCoordinates.Origin.Y - offsetY - centerY)) / centerY,
                0.0f);

            vertices[3].TextureCoordinates = vertices[2].TextureCoordinates;
            vertices[4].TextureCoordinates = vertices[1].TextureCoordinates;
        }


        public record DisplayDirectXArgs(
            Device Device,
            DeviceContext Context,
            VertexShader VertexShader,
            PixelShader PixelShader,
            InputLayout InputLayout,
            SamplerState LinearSampler);
    }

    
}
