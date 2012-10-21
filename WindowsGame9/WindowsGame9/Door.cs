using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WindowsGame9
{
    public class Door
    {
        private int coolDown;

        private bool isOpen;
        public bool IsOpen
        {
            get { return isOpen; }
            set
            {
                isOpen = value;
                coolDown = 1000;
            }
        }

        public Vector2 Position { get; set; }
        public Texture2D Texture { get; set; }

        public bool IsOpening
        {
            get { return coolDown > 0; }
        }
        public void DecreaseCoolDown(int milis)
        {
            if (IsOpening)
                coolDown -= milis;
        }

        public float Angle
        {
            get { return !IsOpen ? 0 : -1.5f; }
        }
    }
}
