using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace WindowsGame9
{
    class PlayerCard
    {
        public Texture2D Texture { get; set; }
        public Rectangle BoundingBox { get; set; }
        public AnimatedTexture AnimatedTexture { get; set; }
    }
}
