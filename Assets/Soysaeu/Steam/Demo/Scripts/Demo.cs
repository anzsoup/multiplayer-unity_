using UnityEngine;
using System.Collections;

namespace Soysaeu.Steam.Demo
{
	/// -----------------------------------------------------------------------------------
	/// [Steam Events]
	///		인스펙터를 통해, 또는 스크립트로 직접 
	///		Steam 객체에 메소드를 등록하여 스팀 이벤트를 받을 수 있다.
	/// -----------------------------------------------------------------------------------
	
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