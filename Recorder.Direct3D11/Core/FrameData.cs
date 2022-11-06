using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorder.Direct3D11.Interop.Direct3D11;
using Silk.NET.DXGI;

namespace Recorder.Direct3D11.Core
{
    public struct FrameData
    {
        public Texture2D Frame;
        public OutduplFrameInfo FrameInfo;
        public Memory<byte> Metadata;
        public uint DirtyRectanglesCount;
        public uint MoveRectanglesCount;
    }
}
