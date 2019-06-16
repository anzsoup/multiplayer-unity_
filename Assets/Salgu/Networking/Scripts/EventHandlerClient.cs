using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Salgu.Networking
{
	public class EventHandlerClient<Protocol> : INetworkEventHandler where Protocol : IPeer, new()
	{
		private int _clientId = -1;
		private int _connectionId = -1;
		private IPeer _serverPeer = null;
		private Action _tempConnectionHandler = null;
		private bool _isDead = false;

		public EventHandlerClient(string remoteHost, int remotePort, Action connectionHandler)
		{
			try
			{
				// 하나의 서버와만 접속 가능.
				// 근데 호스트 열어놓으면 상대쪽에서 나에게 접속하는 것도 됨 ㅎ
				// 즉 실제로는 클라와 서버의 명확한 구분은 없음
				var maxConnection = 1;
				var hostTopology = new HostTopology(NetworkConfig.Config, maxConnection);

				// 랜덤 포트에 바인딩
				_clientId = NetworkTransport.AddHost(hostTopology, 0);

				_tempConnectionHandler = connectionHandler;

				Debug.Log("Client running on localhost.");

				byte error;
				NetworkTransport.Connect(_clientId, remoteHost, remotePort, 0, out error);
			}
			catch (Exception error)
			{
				Debug.LogError(error);
				Stop();
			}
		}

		public void OnConnectEvent(int hostId, int connectionId, int channelId)
		{
			_connectionId = connectionId;
			_serverPeer = Activator.CreateInstance<Protocol>();
			_tempConnectionHandler.Invoke();
			_tempConnectionHandler = null;
			Debug.Log(string.Format("Connect to server. Id : {0}", connectionId));
			_serverPeer.OnCreated(hostId, connectionId);
		}

		public void OnDataEvent(int hostId, int connectionId, int channelId, byte[] buffer, int dataSize)
		{
			if (_serverPeer != null)
			{
				var msg = new Packet(buffer);
				_serverPeer.OnMessage(msg);
			}
			else
			{
				Debug.LogWarning(string.Format("[OnDataEvent] Server peer not found. Id : {0}", connectionId));
			}
		}

		public void OnDisconnectEvent(int hostId, int connectionId, int channelId)
		{
			// Stop() 이 먼저 호출됐다면 이미 Id 필드는 초기화되므로 파라미터를 이용하는게 안전
			if (_serverPeer != null) _serverPeer.OnRemoved();
			// 호출할 필요 없음
			// NetworkTransport.RemoveHost(hostId);
			_serverPeer = null;
			_isDead = true;
			Debug.Log(string.Format("Disconnect from server. Id : {0}", connectionId));
		}

		public void OnError(int hostId, int connectionId, int channelId, NetworkError error)
		{
			OnDisconnectEvent(hostId, connectionId, channelId);
		}

		public void Stop()
		{
			byte error;
			NetworkTransport.Disconnect(_clientId, _connectionId, out error);
			Debug.Log("Client halted.");
		}

		public bool IsDead()
		{
			return _isDead;
		}

		public void OnRemoved()
		{
			NetworkTransport.RemoveHost(_clientId);
		}
	}
}