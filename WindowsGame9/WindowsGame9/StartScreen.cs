using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Threading;
using XnaGameServer;

namespace WindowsGame9
{
    class StartScreen
    {
        SpriteBatch spriteBatch;
        PlayerCard alienCard;
        PlayerCard humanCard;
        Player thisPlayer;
        MapBuilder mapBuilder;
        Texture2D checkedTexture;
        Texture2D unCheckedTexture;
        CheckBox serverCheckBox;
        Thread serverThread;

        public StartScreen(SpriteBatch spriteBatch, PlayerCard humanCard, PlayerCard alienCard, Player thisPlayer, 
            MapBuilder mapBuilder, Texture2D checkedTexture, Texture2D unCheckedTexture, SpriteFont font)
        {
            this.spriteBatch = spriteBatch;
            this.humanCard = humanCard;
            this.alienCard = alienCard;
            this.thisPlayer = thisPlayer;
            this.mapBuilder = mapBuilder;
            this.checkedTexture = checkedTexture;
            this.unCheckedTexture = unCheckedTexture;

            //clientCheckBox = new CheckBox(unCheckedTexture, checkedTexture, "Client", new Rectangle(20, 300, 50, 50), spriteBatch, font);
            //clientCheckBox.IsChecked = true;
            serverCheckBox = new CheckBox(unCheckedTexture, checkedTexture, "Server", new Rectangle(20, 360, 50, 50), spriteBatch, font);
         }

        public void Draw()
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(humanCard.Texture, humanCard.BoundingBox, Color.White);
            spriteBatch.Draw(alienCard.Texture, alienCard.BoundingBox, Color.White);
            spriteBatch.End();

            serverCheckBox.Draw();
        }

        public WindowsGame9.Game1.GameState Update(int elapsedMillis)
        {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                if (alienCard.BoundingBox.Contains(Mouse.GetState().X, Mouse.GetState().Y))
                {
                    thisPlayer.PlayerType = Player.PlayerTypes.alien;
                    thisPlayer.AnimatedTexture = alienCard.AnimatedTexture;
                    thisPlayer.Power = 1000;
                    mapBuilder.BuildMap("map1");
                    return WindowsGame9.Game1.GameState.GameStarted;
                }
                else if (humanCard.BoundingBox.Contains(Mouse.GetState().X, Mouse.GetState().Y))
                {
                    thisPlayer.PlayerType = Player.PlayerTypes.human;
                    thisPlayer.AnimatedTexture = humanCard.AnimatedTexture;
                    mapBuilder.BuildMap("map1");

                    return Game1.GameState.GameStarted;

                }
                //clientCheckBox.CheckClick(Mouse.GetState().X, Mouse.GetState().Y);
                if (serverCheckBox.IsChecked && serverThread == null)
                {
                    serverThread = new Thread(new ThreadStart(XnaServer.Main));
                    serverThread.Start();
                }

                    serverCheckBox.CheckClick(Mouse.GetState().X, Mouse.GetState().Y, elapsedMillis);
            }
            return Game1.GameState.TitleScreen;
        }

    }
}
