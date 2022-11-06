using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorder.Direct3D11.Interop.Direct3D11;
using Recorder.Direct3D11.Interop.DXGI;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;

namespace Recorder.Direct3D11.Core
{
    public class DisplayDuplication
    {
        private byte[] _metadataBuffer = Array.Empty<byte>();
        private Texture2D? _acquiredFrame;

        private readonly Device _device;
        private readonly OutputDuplication _outputDuplication;

        public OutputDesc OutputDescription => _outputDuplication.Parent.Description;

        public DisplayDuplication(Device device, Output output)
        {
            _device = device;
            _outputDuplication = output.DuplicateOutput(device);
        }

        public unsafe FrameData AcquireNextFrame(uint timeoutInMilliseconds, out bool isTimeoutExceeded)
        {
            var result = new FrameData();

            var (frameInfo, dxgiResource) = _outputDuplication.AcquireNextFrame(timeoutInMilliseconds, out var isTimeoutExceededInner);
            isTimeoutExceeded = isTimeoutExceededInner;

            if (isTimeoutExceeded)
                return default;

            if (dxgiResource == null)
                throw new NullReferenceException("Output duplication returned null resource when timeout returned false");

            // Release previous frame
            _acquiredFrame?.Dispose();

            _acquiredFrame = (Texture2D)dxgiResource;
            dxgiResource.Dispose();


            if (frameInfo.TotalMetadataBufferSize > 0)
            {
                if (_metadataBuffer.Length < frameInfo.TotalMetadataBufferSize)
                    _metadataBuffer = new byte[frameInfo.TotalMetadataBufferSize];

                fixed (byte* bufferHandle = _metadataBuffer)
                {
                    var moveRectsHandle = (IntPtr)bufferHandle;
                    _outputDuplication.GetFrameMoveRects((uint)_metadataBuffer.Length, moveRectsHandle,
                        out var moveRectanglesLength);
                    result.MoveRectanglesCount = moveRectanglesLength / (uint)sizeof(OutduplMoveRect);

                    var bufferSize = (uint)_metadataBuffer.Length - moveRectanglesLength;
                    var dirtyRectsHandle = IntPtr.Add(moveRectsHandle, (int)moveRectanglesLength);
                    _outputDuplication.GetFrameDirtyRects(bufferSize, dirtyRectsHandle, out var dirtyRectanglesLength);
                    result.DirtyRectanglesCount = dirtyRectanglesLength / (uint)sizeof(Rectangle<int>);
                }
            }

            result.Frame = _acquiredFrame;
            result.FrameInfo = frameInfo;
            result.Metadata = new Memory<byte>(_metadataBuffer, 0, (int)frameInfo.TotalMetadataBufferSize);

            return result;
        }

        public void ReleaseFrame()
        {
            _outputDuplication.ReleaseFrame();
            _acquiredFrame?.Dispose();
            _acquiredFrame = null;
        }
        
    }
}
