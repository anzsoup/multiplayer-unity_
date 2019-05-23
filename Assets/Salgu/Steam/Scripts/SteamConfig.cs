using UnityEngine;
using System.Collections;

namespace Salgu.Steam
{
	public class SteamConfig
	{
		public string ModDir { get; set; }

		/// <summary>
		/// 스팀서버정보의 게임 이름에 표시되는 정보.
		/// </summary>
		public string GameDescription { get; set; }

		/// <summary>
		/// 스팀서버정보의 버전란에 표시되는 정보.
		/// 서버와 클라이언트가 서로 이 값이 다르면 서버는 클라이언트의 접속을 거부한다.
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// VAC 사용 여부.
		/// </summary>
		public bool Secure { get; set; }

		/// <summary>
		/// 스팀서버정보의 이름란에 표시되는 정보.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 스팀서버의 최대인원.
		/// 최대인원을 초과할 경우 스팀티켓을 받아올 수 없다.
		/// </summary>
		public int MaxPlayers { get; set; }

		public SteamConfig()
		{
			ModDir = "my-game";
			GameDescription = "My Game";
			Version = "1.0";
			Secure = true;
			Name = "My Game Server";
			MaxPlayers = 20;
		}
	}
}