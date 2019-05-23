using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Salgu.Networking
{
	public class EventHandlerServer<Protocol> : INetworkEventHandler where Protocol : IPeer, new()
	{
		private int _serverId = -1;
		private int _maxConnection = 1;
		private readonly Dictionary<int, IPeer> _peerDict = new Dictionary<int, IPeer>();
		private bool _reserveTerminate = false;
		
		public EventHandlerServer(int port, int maxConnection)
		{
			try
			{
				_maxConnection = Mathf.Max(_maxConnection, maxConnection);
				HostTopology topology = new HostTopology(NetworkConfig.Config, _maxConnection);

				// 서버는 통신을 위한 호스트를 단 1개 가진다.
				// 따라서 모든 네트워크 이벤트는 이 호스트 ID와 함께 전달된다.
				_serverId = NetworkTransport.AddHost(topology, port);

				Debug.Log(string.Format("Server running localhost. Port : {0}", port));
			}
			catch (Exception error)
			{
				Debug.LogError(error);
				Stop();
			}
		}

		public void OnConnectEvent(int hostId, int connectionId, int channelId)
		{
			try
			{
				// 유니티가 주는 Connection ID가 중복되는 일은 존재하지 않는다.
				// 다만 접속이 끊어지면 제때제때 딕셔너리의 내용물을 비워야 한다.
				var newClient = Activator.CreateInstance<Protocol>();
				_peerDict.Add(connectionId, newClient);
				Debug.Log(string.Format("Client connected. Id : {0}", connectionId));
				newClient.OnCreated(hostId, connectionId);
			}
			catch (Exception error)
			{
				Debug.LogError(error);
			}
		}

		public void OnDataEvent(int hostId, int connectionId, int channelId, byte[] buffer, int dataSize)
		{
			try
			{
				var peer = _peerDict[connectionId];
				var msg = new Packet(buffer);
				peer.OnMessage(msg);
			}
			catch (Exception error)
			{
				Debug.LogError(error);
			}
		}

		public void OnDisconnectEvent(int hostId, int connectionId, int channelId)
		{
			// Stop() 이 먼저 호출됐다면 이미 Id 필드는 초기화되므로 파라미터를 이용하는게 안전
			try
			{
				var peer = _peerDict[connectionId];
				peer.OnRemoved();
				_peerDict.Remove(connectionId);
				// 호출할 필요 없음
				// NetworkTransport.RemoveHost(connectionId);
				Debug.Log(string.Format("Client disconnected. Id : {0}", connectionId));
			}
			catch (Exception error)
			{
				Debug.LogError(error);
			}
		}

		public void OnError(int hostId, int connectionId, int channelId, NetworkError error)
		{
			OnDisconnectEvent(hostId, connectionId, channelId);
		}

		public void Stop()
		{
			foreach (var connectionId in _peerDict.Keys.ToList())
			{
				byte error;
				NetworkTransport.Disconnect(_serverId, connectionId, out error);
			}
			_reserveTerminate = true;
			Debug.Log("Server halted.");
		}

		public bool IsDead()
		{
			return _reserveTerminate && (_peerDict.Count == 0);
		}

		public void OnRemoved()
		{
			NetworkTransport.RemoveHost(_serverId);
		}
	}
}