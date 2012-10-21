using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace WindowsGame9
{
    public class Weapon
    {
        public Weapon()
        {
            DrainsPower = true;
        }

        public string Name { get; set; }
        public int Range { get; set; }
        public int Damage { get; set; }

        public int Power { get; set; }
        public bool IsPrimary { get; set; }
        public Texture2D Texture { get; set; }
        public int Push { get; set; }
        
        private int coolDown;
        public int CoolDown
        {
            get { return coolDown; }
            set { coolDown = value; ResetCoolDown(); }
        }

        public int CurrentCoolDown { get; set; }

        public AnimatedTexture UserTexture { get; set; }
        public AnimatedTexture EffectTexture { get; set; }

        public void ResetCoolDown()
        {
            CurrentCoolDown = CoolDown;
        }

        public Bomb Bomb { get; set; }
        public bool DrainsPower { get; set; }
        
        public Vector2 Position { get; set; }
        public Player.PlayerTypes UsedByType { get; set; }

        public Rectangle BoundingBox
        {
            get { return new Rectangle((int)Position.X * 20, (int)Position.Y * 20, 40, 40); }
        }

        public bool IsInit { get; set; }

        public Weapon Clone()
        {
            Weapon weapon = new Weapon
            {
                Power = Power,
                IsPrimary = IsPrimary,
                Texture = Texture,
                Name = Name,
                CoolDown = CoolDown,
                DrainsPower = DrainsPower,
                Damage = Damage,
                Range = Range,
                Push = Push
            };

            if (UserTexture != null)
                weapon.UserTexture = UserTexture.Clone();

            if (EffectTexture != null)
                weapon.EffectTexture = EffectTexture.Clone();

            return weapon;

        }

    }
}
