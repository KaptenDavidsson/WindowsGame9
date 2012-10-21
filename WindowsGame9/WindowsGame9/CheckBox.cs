using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace WindowsGame9
{
    class CheckBox
    {
        SpriteBatch spriteBatch;
        Texture2D unCheckedTexture;
        Texture2D checkedTexture;
        string text;
        SpriteFont font;
        Rectangle boundingBox;
        int coolDown;

        public CheckBox(Texture2D unCheckedTexture, Texture2D checkedTexture, string text, Rectangle boundingBox, SpriteBatch spriteBatch, SpriteFont font)
        {
            this.unCheckedTexture = unCheckedTexture;
            this.checkedTexture = checkedTexture;
            this.text = text;
            this.boundingBox = boundingBox;
            this.spriteBatch = spriteBatch;
            this.font = font;
        }

        public bool IsChecked { get; set; }
        public void CheckClick(int x, int y, int elapsedMillis)
        {
            if (boundingBox.Contains(x, y) && coolDown <= 0)
            {
                IsChecked = !IsChecked;
                coolDown = 500;
            }

            if (coolDown > 0)
                coolDown -= elapsedMillis;
        }

        public void Draw()
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(IsChecked ? checkedTexture : unCheckedTexture, boundingBox, Color.White);
            spriteBatch.DrawString(font, text, new Vector2(boundingBox.X + 70, boundingBox.Y + 10), Color.Red);
            spriteBatch.End();

        }
    }
}
