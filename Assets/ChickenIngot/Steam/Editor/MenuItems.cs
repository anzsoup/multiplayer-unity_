using UnityEditor;
using UnityEngine;

namespace ChickenIngot.Steam
{
	public class MenuItems : MonoBehaviour
	{
		[MenuItem("GameObject/Steam Service", priority = 30)]
		static void CreateRMPUnityService()
		{
			new GameObject("Steam Service", typeof(SteamService));
		}
	}
}