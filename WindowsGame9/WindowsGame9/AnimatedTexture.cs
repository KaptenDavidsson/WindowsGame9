using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace WindowsGame9
{
    public class AnimatedTexture
    {
        int horizontal;
        int vertical;
        int step;
        int sheetWidth;
        int sheetHeight;
        int width;
        int height;
        bool repeat;
        int framesPerSecond;
        SpriteBatch spriteBatch;
        Vector2 origin;
        SoundEffect soundEffect;

        public AnimatedTexture(Texture2D spriteSheet, int horizontal, int vertical, bool repeat, int framesPerSecond, SpriteBatch spriteBatch, 
            int width, int height, Vector2 origin, SoundEffect soundEffect)
        {
            this.SpriteSheet = spriteSheet;
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.sheetWidth = spriteSheet.Width / horizontal;
            this.sheetHeight = spriteSheet.Height / vertical;
            this.repeat = repeat;
            this.framesPerSecond = framesPerSecond;
            this.spriteBatch = spriteBatch;
            this.origin = origin;
            this.width = width; ;
            this.height = height;
            this.soundEffect = soundEffect;
        }

        int millisencondsSinceLast;
        public Rectangle GetSource(int elapsedMiliseconds)
        {
            if (elapsedMiliseconds + millisencondsSinceLast > 1000 / framesPerSecond)
            {
                step += (elapsedMiliseconds + millisencondsSinceLast) / (1000 / framesPerSecond);
                millisencondsSinceLast = (elapsedMiliseconds + millisencondsSinceLast) % (1000 / framesPerSecond);
            }
            else
            {
                millisencondsSinceLast += elapsedMiliseconds;
            }

            //if (IsDone && repeat)
            //    Reset();
            //if (IsDone)
            //    IsRunning = false;

            if (step > horizontal * vertical && !repeat)
                step = horizontal * vertical;

            if (repeat)
                step %= horizontal * vertical;

            return new Rectangle((step % horizontal) * sheetWidth, (step / horizontal) * sheetHeight, sheetWidth, sheetHeight);
        }

        public void Draw(int elapsedMilliseconds, Vector2 position, Vector2 direction)
        {
            spriteBatch.Draw(SpriteSheet, new Rectangle((int)position.X, (int)position.Y, width, height), GetSource(elapsedMilliseconds), Color.White, (float)-Math.Atan2(direction.X, direction.Y), origin, new SpriteEffects(), 0f);
        }

        public Rectangle GetSourceFromFrameNbr(int frameNbr)
        {
            return new Rectangle((frameNbr % horizontal) * sheetWidth, (frameNbr / horizontal) * sheetHeight, sheetWidth, sheetHeight);
        }

        public void DrawFrame(int frameNbr, Vector2 position, Vector2 direction)
        {
            spriteBatch.Draw(SpriteSheet, new Rectangle((int)position.X, (int)position.Y, width, height), GetSourceFromFrameNbr(frameNbr), Color.White, (float)-Math.Atan2(direction.X, direction.Y), origin, new SpriteEffects(), 0f);
        }

        public Texture2D SpriteSheet{ get; private set; }
        public bool IsDone
        {
            get { return step == horizontal * vertical; }
        }
        public void Reset()
        {
            step = 0;
        }

        public void Start()
        {
            step = 0;
            //IsRunning = true;

            //if (soundEffect != null)
            //    soundEffect.Play();
        }
        //public bool IsRunning { get; set; }

        public AnimatedTexture Clone()
        {
            return new AnimatedTexture(SpriteSheet, horizontal, vertical, repeat, framesPerSecond, spriteBatch, width, height, origin, soundEffect);
        }

    }
}
