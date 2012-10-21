using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XnaGameServer
{
    public class Damage
    {
        public byte MessageRout
        {
            get { return 1; }
        }
        public Int64 PlayerId { get; set; }
        public byte WeaponId { get; set; }
        public byte Angle { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
