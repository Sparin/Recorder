using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;

namespace Recorder.Direct3D11.Interop.Direct3D
{
    public struct Vertex
    {
        public Vector3D<float> Position { get; set; }
        public Vector2D<float> TextureCoordinates { get; set; }
    }
}
