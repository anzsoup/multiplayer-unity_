using System;
using UnityEngine.Events;

namespace Salgu.Networking
{
	// Server side
	[Serializable]
	public class StartServerEvent : UnityEvent { }
	[Serializable]
	public class StopServerEvent : UnityEvent { }
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