﻿using System;
using System.Linq;
using System.Threading;
using Rug.Osc;

namespace LaptopOrchestra.Kinect
{
	public class UDPReceiver
	{
		private OscReceiver _receiver;
		private int _port;
		private Thread _listenerThread;
		private SessionManager _sessionManager;
		private KinectProcessor _dataPub;

		public UDPReceiver(int port, SessionManager sessionManager, KinectProcessor dataPub)
		{
			_port = port;
			_sessionManager = sessionManager;
			_dataPub = dataPub;
			_receiver = new OscReceiver(_port);
			_listenerThread = new Thread(new ThreadStart(ListenerWork));
			_receiver.Connect();
			_listenerThread.Start();
		}

		private void ListenerWork()
		{
			while (_receiver.State != OscSocketState.Closed)
			{

				// if we are in a state to recieve
				if (_receiver.State == OscSocketState.Connected)
				{
					// get the next message 
					// this will block until one arrives or the socket is closed
					OscPacket packet = _receiver.Receive();

					// parse the message
					string[] msg = OscDeserializer.ParseOscPacket(packet);
					string[] msgAddress = OscDeserializer.GetMessageAddress(msg);
					var ip = OscDeserializer.GetMessageIp(msg);
					var port = OscDeserializer.GetMessagePort(msg);
					var binSeq = OscDeserializer.GetMessageBinSeq(msg);

					// if the sessionWorker already exists, update config. Otherwise, create a new sessionWorker
					SessionWorker session = CheckSession(ip, port);

					if (session == null)
					{
						session = new SessionWorker(ip, port, _dataPub, _sessionManager);
						_sessionManager.AddConnection(session);
						session.SetTimers();
					}
					session.SetLookupFlags(binSeq);
				}
			}
		}

		private SessionWorker CheckSession(string ip, int port)
		{
			SessionWorker session = _sessionManager.OpenConnections.FirstOrDefault(x => x.Ip == ip && x.Port == port);
			return session;
		}

		public void Close()
		{
			_receiver.Close();

			_listenerThread.Join();
		}
	}
}
