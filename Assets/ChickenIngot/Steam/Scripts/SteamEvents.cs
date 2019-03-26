using UnityEngine.Events;
using System;

namespace ChickenIngot.Steam
{
	[Serializable]
	public class SteamUserJoinEvent : UnityEvent<SteamUser> { }
	[Serializable]
	public class SteamUserExitEvent : UnityEvent<SteamUser> { }
	[Serializable]
	public class SteamServerOpenEvent : UnityEvent { }
	[Serializable]
	public class SteamServerCloseEvent : UnityEvent { }
	[Serializable]
	public class JoinSteamServerEvent : UnityEvent { }
	[Serializable]
	public class ExitSteamServerEvent : UnityEvent { }
}
