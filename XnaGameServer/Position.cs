using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XnaGameServer
{
    class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Angle { get; set; }
        public bool IsShooting { get; set; }
        public bool IsHuman { get; set; }
    }
}
