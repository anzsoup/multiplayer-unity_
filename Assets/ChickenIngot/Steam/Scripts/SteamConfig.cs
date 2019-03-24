using UnityEngine;
using System.Collections;

public class SteamConfig
{
	public string ModDir { get; set; }

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
