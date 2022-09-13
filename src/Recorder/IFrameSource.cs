using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder
{
    internal interface IFrameSource
    {
        int Height { get; }
        int Width { get; }
        ReadOnlyMemory<byte> AcquireNextFrame();
    }
}
