using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WindowsGame9
{
    public class ServerLayer
    {
        public enum MessageRouts
        {
            Position,
            Fire,
            MoveNpc,
            DiscoveryResponse,
            None
        }

        NetClient client;
        NetIncomingMessage msg;

        public ServerLayer()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            client = new NetClient(config);
            client.Start();
            client.DiscoverLocalPeers(14242);
        }

        public void SendPosition(Player player)
        {
            NetOutgoingMessage om = client.CreateMessage();
            om.Write((byte)0);
            om.Write((int)player.Position.X);
            om.Write((int)player.Position.Y);
            om.Write((int)(-Math.Atan2(player.Direction.X, player.Direction.Y) * 1000));
            om.Write(!player.SelectedPrimaryWeapon.UserTexture.IsDone);
            om.Write(player.IsHuman);
            client.SendMessage(om, NetDeliveryMethod.Unreliable);
        }

        public void SendShoting(Player thisPlayer, Int64 shotPlayerId, List<Weapon> weaponDefinitions, Weapon weapon, int x, int y)
        {
            NetOutgoingMessage om = client.CreateMessage();
            om.Write((byte)MessageRouts.Fire);
            om.Write((Int64)shotPlayerId);
            om.Write((byte)weaponDefinitions.IndexOf(weaponDefinitions.SingleOrDefault(w => w.Name == weapon.Name)));
            om.Write((byte)((-Math.Atan2(thisPlayer.Direction.X, thisPlayer.Direction.Y) * 40)));
            om.Write((int)x);
            om.Write((int)y);
            client.SendMessage(om, NetDeliveryMethod.Unreliable);
        }

        public MessageRouts MessageRout
        {
            get
            {
                msg = client.ReadMessage();
                if (msg == null) return MessageRouts.None;

                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.DiscoveryResponse: return MessageRouts.DiscoveryResponse;
                    case NetIncomingMessageType.Data: return (MessageRouts)msg.ReadByte();
                    default: return MessageRouts.None;
                }

            }
        }

        public void GetPosition(Dictionary<Int64, Player> players, List<Weapon> weaponDefinitions, Game1 forLoadingContent, SpriteBatch spriteBatch)
        {
            long who = msg.ReadInt64();
            int x = msg.ReadInt32();
            int y = msg.ReadInt32();
            int angle = msg.ReadInt32();
            bool isShooting = msg.ReadBoolean();
            bool isHuman = msg.ReadBoolean();
            if (players.ContainsKey(who))
            {
                players[who].Position = new Vector2(x, y);
                players[who].Direction = new Vector2(-(float)Math.Sin(((float)angle) / 1000), (float)Math.Cos(((float)angle) / 1000));
            }
            else
            {
                players[who] = new Player { PlayerType = isHuman ? Player.PlayerTypes.human : Player.PlayerTypes.alien,
                                            AnimatedTexture = new AnimatedTexture(isHuman ? forLoadingContent.Content.Load<Texture2D>("PlayerSprites/dudespritesheet") : forLoadingContent.Content.Load<Texture2D>("PlayerSprites/alienspritesheet"), 4, 4, true, 20, spriteBatch, 50, 50, new Vector2(25, 25), null),
                                            Id = who
                };
                Weapon weaponDefinition = weaponDefinitions.SingleOrDefault(w => w.Name == "gun");

                players[who].AddWeapon(weaponDefinition.Clone());
            }
        }

        public void GetFire(Dictionary<Int64, Player> players, List<Weapon> weaponDefinitions, Player thisPlayer, PlayerBase bloodNpc, AnimatedTexture bloodTexture, List<Bomb> bombs)
        {
            long who = msg.ReadInt64();
            Int64 playerId = msg.ReadInt64();
            byte weaponId = msg.ReadByte();
            byte angle = msg.ReadByte();
            int x = msg.ReadInt32();
            int y = msg.ReadInt32();
            Weapon weaponDefinition = weaponDefinitions[weaponId];
            Weapon weapon = players[who].primaryWeapons.SingleOrDefault(w => w.Name == weaponDefinition.Name) ?? players[who].secondaryWeapons.SingleOrDefault(w => w.Name == weaponDefinition.Name);

            if (weapon == null)
            {
                weapon = weaponDefinition.Clone();
                players[who].AddWeapon(weapon);
            }
            players[who].SelectWeapon(weapon);

            if (weapon.IsPrimary)
            {
                if (players[who].SelectedPrimaryWeapon.UserTexture.IsDone)
                    players[who].SelectedPrimaryWeapon.UserTexture.Start();

                if (players[who].SelectedPrimaryWeapon.EffectTexture.IsDone)
                    players[who].SelectedPrimaryWeapon.EffectTexture.Start();

                thisPlayer.TakeDamage(players[who].SelectedPrimaryWeapon.Damage,
                    new Vector2((float)-Math.Sin(((double)angle) / 40), (float)Math.Cos(((double)angle) / 40)), 
                    players[who].SelectedPrimaryWeapon.Push);

                thisPlayer.Life -= weapon.Damage;
                thisPlayer.Position = thisPlayer.Position - new Vector2((float)-Math.Sin(((double)angle) / 40), (float)Math.Cos(((double)angle) / 40)) * 50;
                bloodNpc = thisPlayer;
                bloodTexture.Start();
            }
            else
            {
                Vector2 vel = new Vector2(x, y) - thisPlayer.ScreenCenter;
                vel.Normalize();

                vel *= 10;
                if (weapon.Name == "grenade")
                {
                    bombs.Add(new Bomb(players[who].SelectedSecondaryWeapon.UserTexture, players[who].SelectedSecondaryWeapon.EffectTexture,
                                        30, 35, 10, new Vector2(players[who].Position.X + thisPlayer.ScreenCenter.X, players[who].Position.Y + thisPlayer.ScreenCenter.Y),
                                        vel, 500, 500, 100, true, new Vector2(x, y) + players[who].Position));
                }
                else
                {
                    if (players[who].SelectedSecondaryWeapon.Bomb == null)
                    {
                        Bomb bomb = new Bomb(players[who].SelectedSecondaryWeapon.UserTexture, players[who].SelectedSecondaryWeapon.EffectTexture,
                            30, 35, 10, new Vector2(players[who].Position.X + thisPlayer.ScreenCenter.X, players[who].Position.Y + thisPlayer.ScreenCenter.Y),
                            Vector2.Zero, 50, 500, 500, false, Vector2.Zero);
                        players[who].SelectedSecondaryWeapon.Bomb = bomb;
                        bombs.Add(bomb);
                    }
                    else
                    {
                        players[who].SelectedSecondaryWeapon.Bomb.Explode();
                        players[who].SelectedSecondaryWeapon.Bomb = null;
                    }

                }
            }
        }

        public void GetNpcPosition(List<PlayerBase> npcs)
        {
            byte index = msg.ReadByte(); 
            int x = msg.ReadInt32();
            int y = msg.ReadInt32();

            if (npcs.Count > index)
                npcs[index].Position = new Vector2(x, y);

        }

        public void Connect()
        {
            client.Connect(msg.SenderEndpoint);
        }

        public void ShutDown()
        {
            client.Shutdown("bye");

        }
    }
}
