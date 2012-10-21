using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace WindowsGame9
{
    public class Bomb
    {
        AnimatedTexture staticTexture;
        AnimatedTexture explosionTexture;
        int damage;
        int areaOfEffect;
        int counter;
        int explotionDuration;
        bool isTimeBomb;
        int width;
        int height;
        Vector2 explodePos;

        public Bomb(AnimatedTexture staticTexture, AnimatedTexture explosionTexture, int width, int height, int damage, Vector2 position, 
            Vector2 velocity, int areaOfEffect, int counter, int explotionDuration, bool isTimeBomb, Vector2 explodePos)
        {
            this.staticTexture = staticTexture;
            this.explosionTexture = explosionTexture;
            this.width = width;
            this.height = height;
            this.damage = damage;
            this.Position = position;
            this.Velocity = velocity;
            this.areaOfEffect = areaOfEffect;
            this.counter = counter;
            this.explotionDuration = explotionDuration;
            this.isTimeBomb = isTimeBomb;
            this.explodePos = explodePos;
        }

        public int Width 
        { 
            get { return counter > 0 ? width : 100;}
        }
        
        public int Height
        { 
            get { return counter > 0 ? height: 100;}
        }
        public Vector2 Position { get; set; }

        public bool Done()
        {
            return counter < -explotionDuration;
        }

        public void Explode()
        {
            counter = 0;
        }
        public Vector2 Velocity { get; set; }

        public bool IsExploding
        {
            get { return counter <= 0; }
        }
        public void Draw(int elapsedMilliseconds, Vector2 thisPlayerPosition)
        {
            if (isTimeBomb || IsExploding)
                counter -= elapsedMilliseconds;

            if (counter > 0)
                Position += Velocity * elapsedMilliseconds / 10;

            if ((Position - explodePos).Length() < 40 && counter > 0)
                Explode();
            //else if (counter == 0)
            //    explosionTexture.Start();

            if (counter > 0)
                staticTexture.Draw(elapsedMilliseconds, Position - thisPlayerPosition, new Vector2());
            else 
                explosionTexture.Draw(elapsedMilliseconds, Position - thisPlayerPosition + new Vector2(0, 50), new Vector2());

        }

    }
}
