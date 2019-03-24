using System;
using UnityEngine.Events;

namespace ChickenIngot.Networking
{
	// Server side
	[Serializable]
	public class ServerOpenEvent : UnityEvent { }
	[Serializable]
	public class ServerCloseEvent : UnityEvent { }
	[Serializable]
	public class ClientConnectEvent : UnityEvent<RMPPeer> { }
	[Serializable]
	public class ClientDisconnectEvent : UnityEvent<RMPPeer> { }

	// Client side
	[Serializable]
	public class ConnectToServerEvent : UnityEvent<RMPPeer> { }
	[Serializable]
	public class DisconnectFromServerEvent : UnityEvent<RMPPeer> { }
}