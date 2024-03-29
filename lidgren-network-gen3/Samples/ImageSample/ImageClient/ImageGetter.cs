﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Lidgren.Network;
using SamplesCommon;

namespace ImageClient
{
	public partial class ImageGetter : Form
	{
		public NetClient Client;
		public byte[] Buffer = new byte[990];
		public bool[] ReceivedSegments;
		public int NumReceivedSegments;

		private double m_startedFetching;
		private List<NetIncomingMessage> m_readList;

		public ImageGetter(string host, NetPeerConfiguration copyConfig)
		{
			InitializeComponent();

			NetPeerConfiguration config = copyConfig.Clone();
			config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
			m_readList = new List<NetIncomingMessage>();

			Client = new NetClient(config);
			Client.Start();

			if (!string.IsNullOrEmpty(host))
			{
				Client.Connect(host, 14242, GetApproveData());
			}
			else
			{
				Client.DiscoverLocalPeers(14242);
			}
		}

		private NetOutgoingMessage GetApproveData()
		{
			// create approval data
			NetOutgoingMessage approval = Client.CreateMessage();
			approval.Write(42);
			approval.Write("secret");
			return approval;
		}

		public void Heartbeat()
		{
			int numRead = Client.ReadMessages(m_readList);
			if (numRead < 1)
				return;

			foreach(var inc in m_readList)
			{
				switch(inc.MessageType)
				{
					case NetIncomingMessageType.DiscoveryResponse:
						// found server! just connect...
						string serverResponseHello = inc.ReadString();
						Client.Connect(inc.SenderEndpoint, GetApproveData());
						break;
					case NetIncomingMessageType.DebugMessage:
					case NetIncomingMessageType.VerboseDebugMessage:
					case NetIncomingMessageType.WarningMessage:
					case NetIncomingMessageType.ErrorMessage:
						string str = inc.ReadString();
						NativeMethods.AppendText(richTextBox1, str);
						//System.IO.File.AppendAllText("C:\\tmp\\clientlog.txt", str + Environment.NewLine);
						break;
					case NetIncomingMessageType.StatusChanged:
						NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
						string reason = inc.ReadString();
						NativeMethods.AppendText(richTextBox1, "New status: " + status + " (" + reason + ")");
						if (status == NetConnectionStatus.Connected)
							m_startedFetching = NetTime.Now;
						break;
					case NetIncomingMessageType.Data:
						// image data, whee!
						// ineffective but simple data model
						ushort width = inc.ReadUInt16();
						ushort height = inc.ReadUInt16();
						uint segment = inc.ReadVariableUInt32();

						Bitmap bm = pictureBox1.Image as Bitmap;
						if (bm == null)
						{
							bm = new Bitmap(width + 1, height + 1);
							pictureBox1.Image = bm;
							this.Size = new System.Drawing.Size(width + 40, height + 60);
							pictureBox1.SetBounds(12, 12, width, height);
						}
						pictureBox1.SuspendLayout();

						int totalBytes = (width * height * 3);
						if (inc.LengthBytes < totalBytes)
						{
							int wholeSegments = totalBytes / 990;
							int segLen = 990;
							int remainder = totalBytes - (wholeSegments * inc.LengthBytes);
							int totalNumberOfSegments = wholeSegments + (remainder > 0 ? 1 : 0);
							if (segment >= wholeSegments)
								segLen = remainder; // last segment can be shorter

							if (ReceivedSegments == null)
								ReceivedSegments = new bool[totalNumberOfSegments];
							if (ReceivedSegments[segment] == false)
							{
								ReceivedSegments[segment] = true;
								NumReceivedSegments++;
								if (NumReceivedSegments >= totalNumberOfSegments)
								{
									Client.Disconnect("So long and thanks for all the fish!");
								}
							}



							int pixelsAhead = (int)segment * 330;

							int y = pixelsAhead / width;
							int x = pixelsAhead - (y * width);

							for (int i = 0; i < (segLen / 3); i++)
							{
								// set pixel
								byte r = inc.ReadByte();
								byte g = inc.ReadByte();
								byte b = inc.ReadByte();
								Color col = Color.FromArgb(r, g, b);
								bm.SetPixel(x, y, col);
								x++;
								if (x >= width)
								{
									x = 0;
									y++;
								}
							}
						}
						else
						{

							for(int y=0;y<height;y++)
							{
								for (int x = 0; x < width; x++)
								{
									// set pixel
									byte r = inc.ReadByte();
									byte g = inc.ReadByte();
									byte b = inc.ReadByte();
									Color col = Color.FromArgb(r, g, b);
									bm.SetPixel(x, y, col);
								}
							}

							double span = NetTime.Now - m_startedFetching;
							double bytesPerSecond = (double)totalBytes / span;
							NativeMethods.AppendText(richTextBox1, "Fetched at " + NetUtility.ToHumanReadable((long)bytesPerSecond) + " per second");

							Client.Disconnect("So long and thanks for all the fish!");
						}

						pictureBox1.ResumeLayout();
						pictureBox1.Invalidate();
						System.Threading.Thread.Sleep(0);

						break;
				}
			}

			// recycle messages to avoid garbage
			Client.Recycle(m_readList);
			m_readList.Clear();
		}
	}
}
