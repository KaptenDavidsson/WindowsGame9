using System;
using System.Threading;

using Lidgren.Network;

namespace XnaGameServer
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
			config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			config.Port = 14242;

			// create and start server
			NetServer server = new NetServer(config);
			server.Start();

			// schedule initial sending of position updates
			double nextSendUpdates = NetTime.Now;

			// run until escape is pressed

            int i = 0;
			while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
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
                            Console.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
                            if (status == NetConnectionStatus.Connected)
                            {
                                //
                                // A new player just connected!
                                //
                                Console.WriteLine(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + " connected!");
                                msg.SenderConnection.Tag = new object[6];
                            }

                            break;
                        case NetIncomingMessageType.Data:
                            //
                            // The client sent input to the server
                            //
                            byte messageRout = msg.ReadByte();
                            if (messageRout == 0)
                            {
                                int xinput = msg.ReadInt32();
                                int yinput = msg.ReadInt32();
                                int angle = msg.ReadInt32();
                                bool isShooting = msg.ReadBoolean();
                                bool isHuman = msg.ReadBoolean();

                                object[] pos = msg.SenderConnection.Tag as object[];

                                pos[0] = messageRout;
                                pos[1] = xinput;
                                pos[2] = yinput;
                                pos[3] = angle;
                                pos[4] = isShooting;
                                pos[5] = isHuman;

                            }
                            else if (messageRout == 1)
                            {
                                Console.WriteLine("test1");

                                Int64 playerId = msg.ReadInt64();
                                byte weaponId = msg.ReadByte();
                                byte angle = msg.ReadByte();

                                object[] pos = msg.SenderConnection.Tag as object[];

                                pos[0] = messageRout;
                                pos[1] = playerId;
                                pos[2] = weaponId;
                                pos[3] = angle;
                            }
                            else if (messageRout == 2)
                            {
                                Int64 playerId = msg.ReadInt64();
                                byte weaponId = msg.ReadByte();
                                byte angle = msg.ReadByte();

                                object[] pos = msg.SenderConnection.Tag as object[];

                                pos[0] = messageRout;
                                pos[1] = playerId;
                                pos[2] = weaponId;
                                pos[3] = angle;
                            }
                            break;
                    }

					//
					// send position updates 30 times per second
					//

					double now = NetTime.Now;
					if (now > nextSendUpdates)
                    {
						// Yes, it's time to send position updates

						// for each player...
						foreach (NetConnection player in server.Connections)
						{
							foreach (NetConnection otherPlayer in server.Connections)
							{
                                if (player != otherPlayer)
                                {
                                    Console.WriteLine("test2");
                                    // send position update about 'otherPlayer' to 'player'
                                    NetOutgoingMessage om = server.CreateMessage();

                                    // write who this position is for
                                    om.Write(otherPlayer.RemoteUniqueIdentifier);

                                    //if (otherPlayer.Tag == null) 
                                    //    otherPlayer.Tag = new int[5];

                                    object[] pos = otherPlayer.Tag as object[];

                                    if (pos[0] != null)
                                    {
                                        if (((byte)pos[0]) == 0)
                                        {
                                            om.Write((byte)pos[0]);
                                            om.Write((int)pos[1]);
                                            om.Write((int)pos[2]);
                                            om.Write((int)pos[3]);
                                            om.Write((bool)pos[4]);
                                            om.Write((bool)pos[5]);
                                            server.SendMessage(om, player, NetDeliveryMethod.ReliableOrdered);
                                        }
                                        else if (((byte)pos[0]) == 1)
                                        {
                                            Console.WriteLine("test3");

                                            om.Write((byte)pos[0]);
                                            om.Write((Int64)pos[1]);
                                            om.Write((byte)pos[2]);
                                            om.Write((byte)pos[3]);
                                            server.SendMessage(om, player, NetDeliveryMethod.ReliableOrdered);
                                        }
                                        else if (((byte)pos[0]) == 2)
                                        {
                                            om.Write((byte)pos[0]);
                                            om.Write((Int64)pos[1]);
                                            om.Write((byte)pos[2]);
                                            om.Write((byte)pos[3]);

                                        }
                                        // send message
                                    }
                                }
							}
						}

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
