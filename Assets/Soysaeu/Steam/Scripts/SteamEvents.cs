using UnityEngine.Events;
using System;

namespace Soysaeu.Steam
{
	// Server side
	[Serializable]
	public class StartSteamServerEvent : UnityEvent { }
	[Serializable]
	public class StopSteamServerEvent : UnityEvent { }
	[Serializable]
	public class SteamUserJoinEvent : UnityEvent<SteamUser> { }
	[Serializable]
	public class SteamUserExitEvent : UnityEvent<SteamUser> { }

	// Client side
	[Serializable]
	public class JoinSteamServerEvent : UnityEvent { }
	[Serializable]
	public class ExitSteamServerEvent : UnityEvent { }
}
