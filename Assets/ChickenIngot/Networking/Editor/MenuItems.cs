using UnityEngine;
using UnityEditor;

namespace ChickenIngot.Networking
{
	public class MenuItems : MonoBehaviour
	{
		[MenuItem("GameObject/RMP Network Service", priority = 11)]
		static void CreateRMPUnityService()
		{
			new GameObject("RMP Network Service", typeof(RMPNetworkService));
		}
	}
}

