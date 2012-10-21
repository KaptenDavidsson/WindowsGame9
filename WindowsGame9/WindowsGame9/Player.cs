using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WindowsGame9
{
    public class Player : PlayerBase
    {
        public enum States
        {
            Walking,
            Shooting,
            Standing
        }

        public enum PlayerTypes
        {
            human = 0,
            alien = 1
        }

        public Player()
        {
            Bounty = 40;
        }

        public bool IsThisPlayer { get; set; }

        public Vector2 ScreenCenter { get; set; }

        public PlayerTypes PlayerType { get; set; }

        public AnimatedTexture AnimatedTexture { get; set; }

        private Vector2 previousPosition;
        public void Draw(int elapsedMilliseconds, Vector2 position, int sensitivity = 1)
        {
            if (Math.Abs(previousPosition.X - position.X) > sensitivity || Math.Abs(previousPosition.Y - position.Y) > sensitivity)
                if (IsThisPlayer)
                    AnimatedTexture.Draw(elapsedMilliseconds, ScreenCenter, Direction);
                else
                    AnimatedTexture.Draw(elapsedMilliseconds, position, Direction);
            else
                if (IsThisPlayer)
                    AnimatedTexture.DrawFrame(4, ScreenCenter, Direction);
                else
                    AnimatedTexture.DrawFrame(4, position, Direction);

            previousPosition = position;
        }

        public List<Weapon> primaryWeapons = new List<Weapon>();
        public List<Weapon> secondaryWeapons = new List<Weapon>();
        private List<Weapon> weaponCache = new List<Weapon>();

        public void NextWeapon()
        {
            if (IsPrimaryWeaponActive)
                SelectedPrimaryWeapon = primaryWeapons[(primaryWeapons.IndexOf(SelectedPrimaryWeapon) + 1) % primaryWeapons.Count];
            else
                SelectedSecondaryWeapon = secondaryWeapons[(secondaryWeapons.IndexOf(SelectedSecondaryWeapon) + 1) % secondaryWeapons.Count];
        }
        public void PreviousWeapon()
        {
            if (IsPrimaryWeaponActive)
                SelectedPrimaryWeapon = primaryWeapons[(primaryWeapons.IndexOf(SelectedPrimaryWeapon) - 1 + primaryWeapons.Count) % primaryWeapons.Count];
            else
                SelectedSecondaryWeapon = secondaryWeapons[(secondaryWeapons.IndexOf(SelectedSecondaryWeapon) - 1 + secondaryWeapons.Count) % secondaryWeapons.Count];
        }
        public Weapon SelectedPrimaryWeapon { get; private set; }
        public Weapon SelectedSecondaryWeapon { get; private set; }

        public void SelectWeapon(Weapon weapon)
        {
            if (weapon.IsPrimary)
                SelectedPrimaryWeapon = weapon;
            else
                SelectedSecondaryWeapon = weapon;
        }

        public bool IsPrimaryWeaponActive { get; set; }


        public void AddWeapon(Weapon weapon)
        {
            if (weapon.IsPrimary)
            {
                primaryWeapons.Add(weapon);
                if (SelectedPrimaryWeapon == null)
                    SelectedPrimaryWeapon = weapon;
            }
            else
            {
                if (PlayerType == PlayerTypes.human)
                    secondaryWeapons.Add(weapon);
                else
                    weaponCache.Add(weapon);

                if (SelectedSecondaryWeapon == null && secondaryWeapons.Count > 0)
                    SelectedSecondaryWeapon = weapon;

            }
        }

        public void RandomizeFromWeaponCache()
        {
            Random random = new Random();
            Weapon weapon = weaponCache[random.Next(weaponCache.Count)];
            secondaryWeapons.Add(weapon);

            if (SelectedSecondaryWeapon == null && secondaryWeapons.Count > 0)
                SelectedSecondaryWeapon = weapon;

            weaponCache.Remove(weapon);
        }

        public string Name { get; set; }
        public int Power { get; set; }
        public int Team { get; set; }
        public Vector2 Direction { get; set; }

        private int score;
        public int Score
        {
            get { return score; }
            set
            {
                if (PlayerType == PlayerTypes.alien)
                {
                    if (secondaryWeapons.Count <= 0)
                        secondaryWeapons.Add(weaponCache.SingleOrDefault(w => w.Name == "stun"));

                    SelectedSecondaryWeapon = weaponCache.SingleOrDefault(w => w.Name == "stun");
                }
                score = value;
                if (PlayerType == PlayerTypes.alien && score / 10 >= secondaryWeapons.Count)
                    RandomizeFromWeaponCache();
            }
        }
       
        public bool CarriesWeapon(string weapon)
        {
            return primaryWeapons.Any(w => w.Name == weapon) || secondaryWeapons.Any(w => w.Name == weapon);
        }

        public void RechargeWeapon(Recharger recharger)
        {
            Weapon weapon = primaryWeapons.SingleOrDefault(w => w.Name == recharger.Type) ?? secondaryWeapons.SingleOrDefault(w => w.Name == recharger.Type);
            weapon.Power += recharger.Power;
        }

        public Int64 Id { get; set; }
        public bool IsHuman
        {
            get { return PlayerType == PlayerTypes.human; }
        }
    }
}
