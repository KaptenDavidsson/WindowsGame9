using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WindowsGame9
{
    public class PlayerBase
    {
        int damageCooldown;
        int stunCounter;

        public Vector2 Position { get; set; }
        public int Life { get; set; }
        public int Bounty { get; set; }
        public virtual void Draw(int elapsedMilliseconds, Vector2 offset)
        {

        }
        public virtual bool IsDead { get; set; }
        public virtual void Move(Vector2 playerPos, int ElapsedMillis) { }
        public bool IsBombImmune(int elapsedMillis)
        {
            if (damageCooldown > 0)
                damageCooldown -= elapsedMillis;
            
            return damageCooldown > 0;
        }

        public void TakeDamage(int damage, Vector2 hitDirection, int push)
        {
            inertialVelocity = hitDirection * push / 10;
            damageCooldown = 500;
            Life -= damage;
            Stun(500);
        }
        public bool IsStunned(int elapsedMillis)
        {
            if (stunCounter > 0)
                stunCounter -= elapsedMillis;
            
            return stunCounter > 0;
        }
        public void Stun(int stunTime)
        {
            stunCounter = stunTime;
        }

        private Vector2 inertialVelocity;
        public Vector2 InertialVelocity
        {
            get { return stunCounter > 0 ? inertialVelocity : Vector2.Zero; }
            set { inertialVelocity = value; }
        }
    }
}
