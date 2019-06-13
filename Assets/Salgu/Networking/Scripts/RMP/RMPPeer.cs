using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace Salgu.Networking
{
	/// <summary>
	/// RMP 프로토콜을 정의한다.
	/// </summary>
	public abstract class RMPPeer : IPeer
	{
		public enum PeerStatus
		{
			Idle,
			Connected,
			Disconnected,
		}

		public int HostId { get; private set; }
		public int ConnectionId { get; private set; }
		public PeerStatus Status { get; private set; }
		public static RMPPeer ServerPeer { get; protected set; }
		public static Dictionary<int, RMPPeer> ClientPeers { get; protected set; }

		public RMPPeer()
		{
			HostId = -1;
			ConnectionId = -1;
			Status = PeerStatus.Idle;
		}

		private void ReceiveRPC(Packet msg)
		{
			var guid = msg.PopString();
			var methodName = msg.PopString();
			var numOfParams = msg.PopInt32();
			var parameters = new object[numOfParams];

			for (int i = 0; i < numOfParams; ++i)
				parameters[i] = RMPEncoding.PopParameter(msg);

			try
			{
				var target = RMPNetworkView.Get(guid);
				target.SendReflectionMessage(this, methodName, parameters);
			}
			catch (Exception error)
			{
				Debug.LogError(error);
			}
		}

		[ClientOnly]
		private void ReceiveReplicate(Packet msg)
		{
			var index = msg.PopInt32();
			var guid = msg.PopString();

			var view = UnityEngine.Object.Instantiate(RMPNetworkService.ReplicateTable[index]);
			view.ReceiveGuid(guid);

			// 클라는 서버가 보낸 데이터를 받아서 초기화를 할 수 있다
			view.SendReflectionMessage(this, "OnReplicate", msg);
		}

		[ClientOnly]
		private void ReceiveRemove(Packet msg)
		{
			var guid = msg.PopString();

			try
			{
				var target = RMPNetworkView.Get(guid);
				UnityEngine.Object.Destroy(target.gameObject);
			}
			catch (Exception error)
			{
				Debug.LogError(error);
			}
		}

		public virtual void OnCreated(int hostId, int connectionId)
		{
			HostId = hostId;
			ConnectionId = connectionId;
			Status = PeerStatus.Connected;
		}

		public virtual void OnRemoved()
		{
			Status = PeerStatus.Disconnected;
		}

		public void OnMessage(Packet msg)
		{
			var pt = (RMPEncoding.ProtocolId)msg.PopByte();
			switch (pt)
			{
				case RMPEncoding.ProtocolId.RPC:
					ReceiveRPC(msg);
					break;

				case RMPEncoding.ProtocolId.Replicate:
					ReceiveReplicate(msg);
					break;

				case RMPEncoding.ProtocolId.Remove:
					ReceiveRemove(msg);
					break;
			}
		}

		public void SendRPC(RMPNetworkView sender, QosType channel, string methodName, params object[] parameters)
		{
			if (string.IsNullOrEmpty(sender.Guid))
			{
				Debug.LogWarning("RPC Aborted : The Guid is empty.");
				return;
			}

			if (parameters == null)
			{
				Debug.LogWarning("RPC Aborted : Parameters array can not be null.");
				return;
			}

			var msg = new Packet();
			msg.Push((byte)RMPEncoding.ProtocolId.RPC);
			msg.Push(sender.Guid);
			msg.Push(methodName);
			msg.Push(parameters.Length);

			foreach (object param in parameters)
				RMPEncoding.PushParameter(msg, param);

			NetworkService.Send(HostId, ConnectionId, msg);
		}

		[ServerOnly]
		public void SendReplicate(RMPNetworkView view)
		{
			var msg = new Packet();
			msg.Push((byte)RMPEncoding.ProtocolId.Replicate);
			msg.Push(view.ReplicationTableIndex);
			msg.Push(view.Guid);

			// 서버는 클라에 함께 보낼 데이터를 넣는다.
			view.SendReflectionMessage(null, "OnReplicate", msg);

			NetworkService.Send(HostId, ConnectionId, msg);
		}

		[ServerOnly]
		public void SendRemove(RMPNetworkView view)
		{
			var msg = new Packet();
			msg.Push((byte)RMPEncoding.ProtocolId.Remove);
			msg.Push(view.Guid);
			NetworkService.Send(HostId, ConnectionId, msg);
		}
	}
}
