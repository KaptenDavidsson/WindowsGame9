using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WindowsGame9
{
    public class Recharger
    {
        public string Type { get; set; }
        public int Power { get; set; }
        public Vector2 Position { get; set; }
        public Texture2D Texture { get; set; }
    }
}
