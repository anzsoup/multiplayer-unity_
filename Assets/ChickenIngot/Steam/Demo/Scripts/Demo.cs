using UnityEngine;
using System.Collections;

namespace ChickenIngot.Steam.Demo
{
	public class Demo : MonoBehaviour
	{
		public void _OnSteamUserJoin(SteamUser user)
		{
			Debug.LogWarning(string.Format("\t** Steam User Joined. : {0} **", user.Username));
		}

		public void _OnSteamUserExit(SteamUser user)
		{
			Debug.LogWarning(string.Format("\t** Steam User Exited. : {0} **", user.Username));
		}

		public void _OnSteamServerOpen()
		{
			Debug.LogWarning("\t** Steam Server Open **");
		}

		public void _OnSteamServerClose()
		{
			Debug.LogWarning("\t** Steam Server Closed **");
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