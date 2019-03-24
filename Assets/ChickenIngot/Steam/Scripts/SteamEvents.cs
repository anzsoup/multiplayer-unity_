using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace ChickenIngot.Steam
{
	public class SteamUserJoinEvent : UnityEvent<SteamUser> { }
	public class SteamUserExitEvent : UnityEvent<SteamUser> { }
	public class SteamServerOpenEvent : UnityEvent { }
	public class SteamServerCloseEvent : UnityEvent { }
	public class JoinSteamServerEvent : UnityEvent { }
	public class ExitSteamServerEvent : UnityEvent { }
}
