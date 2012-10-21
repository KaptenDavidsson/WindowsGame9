using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WindowsGame9
{
    class Npc : PlayerBase
    {
        AnimatedTexture animatedTexture;
        AnimatedTexture deathTexture;
        Dictionary<long, Player> players;
        Player thisPlayer;
        Vector2 direction;
        float speed = .5f;
        bool deathAnimStarted;
        int damage;

        public Npc(AnimatedTexture animatedTexture, AnimatedTexture deathTexture, Dictionary<long, Player> players, Player thisPlayer, 
            Vector2 position, int life, int damage, int bounty)
        {
            this.animatedTexture = animatedTexture;
            this.deathTexture = deathTexture;
            this.players = players;
            this.thisPlayer = thisPlayer;
            this.Position = position;
            this.Life = life;
            this.damage = damage;
            this.Bounty = bounty;
        }

        private Vector2 previousPosition;
        public override void Draw(int elapsedMilliseconds, Vector2 offset)
        {
            if (Life <= 0)
            {
                if (deathTexture.IsDone && deathAnimStarted)
                    IsDead = true;

                if (deathTexture.IsDone)
                {
                    deathTexture.Start();
                    deathAnimStarted = true;
                }

                deathTexture.Draw(elapsedMilliseconds, Position - offset, direction);

            }
            else if (Math.Abs(previousPosition.X - Position.X) > 0 || Math.Abs(previousPosition.Y - Position.Y) > 0)
            {
                animatedTexture.Draw(elapsedMilliseconds, Position - offset, direction);
            }
            else
            {
                animatedTexture.DrawFrame(0, Position - offset, direction);
            }

            previousPosition = Position;
        }

        public override void Move(Vector2 playerPos, int elapsedMillis)
        {
            if (!IsStunned(elapsedMillis) && !deathAnimStarted)
            {
                direction = -playerPos + Position;
                if (direction.Length() < 500 && direction.Length() > 30)
                {
                    direction.Normalize();
                    Position -= speed * direction;
                }
                else if (direction.Length() <= 30)
                {
                    thisPlayer.Life -= damage;
                }
            }
            else
            {
                Position -= speed * InertialVelocity;

            }
        }

        public override bool IsDead { get; set; }
    }
}
