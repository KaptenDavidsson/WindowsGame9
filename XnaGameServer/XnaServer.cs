using System;
using System.Threading;

using Lidgren.Network;
using System.Collections.Generic;
using System.Diagnostics;

namespace XnaGameServer
{
    enum MessageRouts
    {
        Position,
        Fire,
        MoveNpc
    }

	public class XnaServer
	{
        public static bool IsRunning = true;

		public static void Main()
		{
			NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
			config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			config.Port = 14242;

			// create and start server
			NetServer server = new NetServer(config);
			server.Start();

			// schedule initial sending of position updates
			double nextSendUpdates = NetTime.Now;

            Dictionary<NetConnection, List<Damage>> damages = new Dictionary<NetConnection, List<Damage>>();
            Dictionary<NetConnection, Position> positions = new Dictionary<NetConnection, Position>();
            List<Npc> npcs = new List<Npc>();
            npcs.Add(new Npc { X = 1700, Y = 1000, Speed = 4, Life = 3 });

			// run until escape is pressed

            int i = 0;
			while (IsRunning)
			{
				NetIncomingMessage msg;
				while ((msg = server.ReadMessage()) != null)
				{
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.DiscoveryRequest:
                            //
                            // Server received a discovery request from a client; send a discovery response (with no extra data attached)
                            //
                            server.SendDiscoveryResponse(null, msg.SenderEndpoint);
                            break;
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            //
                            // Just print diagnostic messages to console
                            //
                            //Console.WriteLine(msg.ReadString());
                            Debug.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
                            if (status == NetConnectionStatus.Connected)
                            {
                                //
                                // A new player just connected!
                                //
                                Console.WriteLine(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + " connected!");
                                Debug.WriteLine(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + " connected!");
                                msg.SenderConnection.Tag = new object[6];
                            }

                            break;
                        case NetIncomingMessageType.Data:
                            //
                            // The client sent input to the server
                            //
                            MessageRouts messageRout = (MessageRouts)msg.ReadByte();
                            switch (messageRout)
                            {
                                case MessageRouts.Position:
                                    Position position = new Position();

                                    position.X = msg.ReadInt32();
                                    position.Y = msg.ReadInt32();
                                    position.Angle = msg.ReadInt32();
                                    position.IsShooting = msg.ReadBoolean();
                                    position.IsHuman = msg.ReadBoolean();

                                    positions[msg.SenderConnection] = position;
                                    break;
                                case MessageRouts.Fire:
                                    Damage damage = new Damage();

                                    damage.PlayerId = msg.ReadInt64();
                                    damage.WeaponId = msg.ReadByte();
                                    damage.Angle = msg.ReadByte();
                                    damage.X = msg.ReadInt32();
                                    damage.Y = msg.ReadInt32();

                                    if (damage.PlayerId > 50)
                                    {
                                        if (!damages.ContainsKey(msg.SenderConnection))
                                            damages[msg.SenderConnection] = new List<Damage>();

                                        damages[msg.SenderConnection].Add(damage);
                                    }
                                    else
                                    {
                                        npcs[(int)damage.PlayerId].Life -= 1;
                                        if (npcs[(int)damage.PlayerId].Life <= 0)
                                            npcs.RemoveAt((int)damage.PlayerId);
                                    }
                                    break;
                            }
                            break;
                    }

					// send position updates 30 times per second
					double now = NetTime.Now;
					if (now > nextSendUpdates)
                    {
                        foreach (var position in positions.Values)
                        {
                            foreach (var npc in npcs)
                            {
                                float velX = position.X - npc.X;
                                float velY = position.Y - npc.Y;
                                float distance = (float)Math.Sqrt(Math.Pow(velX, 2) + Math.Pow(velY, 2));
                                if (distance < 300 && distance > 30)
                                {
                                    velX /= distance;
                                    velY /= distance;

                                    npc.X += (int)(velX * npc.Speed);
                                    npc.Y += (int)(velY * npc.Speed);

                                    NetOutgoingMessage om2 = server.CreateMessage();
                                    om2.Write((byte)MessageRouts.MoveNpc);
                                    om2.Write((byte)npcs.IndexOf(npc));
                                    om2.Write((int)npc.X);
                                    om2.Write((int)npc.Y);

                                    foreach (NetConnection player in server.Connections)
                                        server.SendMessage(om2, player, NetDeliveryMethod.ReliableUnordered);
                                }

                            }
                        }

						foreach (NetConnection player in server.Connections)
						{
                            foreach (var damagePair in damages)
                            {
                                if (damagePair.Key != player)
                                {
                                    foreach (var damage in damagePair.Value)
                                    {
                                        NetOutgoingMessage om2 = server.CreateMessage();
                                        om2.Write((byte)MessageRouts.Fire);
                                        om2.Write(damagePair.Key.RemoteUniqueIdentifier);
                                        om2.Write(damage.PlayerId);
                                        om2.Write(damage.WeaponId);
                                        om2.Write(damage.Angle);
                                        om2.Write(damage.X);
                                        om2.Write(damage.Y);

                                        server.SendMessage(om2, player, NetDeliveryMethod.ReliableUnordered);
                                    }
                                }
                            }

                            foreach (NetConnection otherPlayer in server.Connections)
                            {
                                if (player != otherPlayer)
                                {
                                    NetOutgoingMessage om = server.CreateMessage();

                                    if (positions.ContainsKey(otherPlayer))
                                    {
                                        om.Write((byte)MessageRouts.Position);
                                        om.Write(otherPlayer.RemoteUniqueIdentifier);
                                        om.Write(positions[otherPlayer].X);
                                        om.Write(positions[otherPlayer].Y);
                                        om.Write(positions[otherPlayer].Angle);
                                        om.Write(positions[otherPlayer].IsShooting);
                                        om.Write(positions[otherPlayer].IsHuman);
                                        server.SendMessage(om, player, NetDeliveryMethod.Unreliable);
                                    }

                                    // send message
                                }
                            }
						}

                        foreach (var damageList in damages.Values)
                            damageList.Clear();

						// schedule next update
						nextSendUpdates += (1.0 / 30.0);
					}
				}

				// sleep to allow other processes to run smoothly
				Thread.Sleep(1);
			}

			server.Shutdown("app exiting");
		}
	}
}
