using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recorder
{
    public struct FrameDescription
    {
        public ulong Width { get; }
        public ulong Height { get; }

        public FrameDescription(ulong width, ulong height)
        {
            Width = width;
            Height = height;
        }
    }
}
