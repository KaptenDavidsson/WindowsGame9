using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WindowsGame9
{
    class Staircase
    {
        public Rectangle BoundingBox { get; set; }
        public Texture2D Texture { get; set; }
        public int Floor { get; set; }
        public int ToFloor { get; set; }
    }
}
