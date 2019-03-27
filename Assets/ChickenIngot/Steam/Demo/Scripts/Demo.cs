using UnityEngine;
using System.Collections;

namespace ChickenIngot.Steam.Demo
{
	public class Demo : MonoBehaviour
	{
		public void _OnStartSteamServer()
		{
			Debug.LogWarning("\t** Start Steam Server **");
		}

		public void _OnStopSteamServer()
		{
			Debug.LogWarning("\t** Stop Steam Server **");
		}

		public void _OnSteamUserJoin(SteamUser user)
		{
			Debug.LogWarning(string.Format("\t** Steam User Joined. : {0} **", user.Username));
		}

		public void _OnSteamUserExit(SteamUser user)
		{
			Debug.LogWarning(string.Format("\t** Steam User Exited. : {0} **", user.Username));
		}

		public void _OnJoinSteamServer()
		{
			Debug.LogWarning("\t** Join Steam Server **");
		}

		public void _OnExitSteamServer()
		{
			Debug.LogWarning("\t** Exit Steam Server **");
		}
	}
}