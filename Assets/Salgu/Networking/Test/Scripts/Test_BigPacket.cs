using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Salgu.Networking.Test
{
	public class Test_BigPacket : MonoBehaviour
	{
		[SerializeField] RMPNetworkView _view;
		[SerializeField] bool _isClient;

		IEnumerator Start()
		{
			while (!RMPNetworkService.IsInitialized)
				yield return null;

			if (_isClient)
			{
				RMPNetworkService.OnConnectToServer.AddListener(OnConnected);
				RMPNetworkService.StartClient("127.0.0.1", 9999);
			}
			else
			{
				RMPNetworkService.StartServer(9999);
			}
		}

		void OnConnected(RMPPeer server)
		{
			var big = new byte[10000];
			for (int i = 0; i < 10000; ++i)
				big[i] = (byte)(i % 256);

			_view.RPC(RPCOption.ToServer, "Send", big);
		}

		void Send(byte[] data)
		{
			Debug.Log(data.Length);
			string str = "";
			for (int i = 0; i < data.Length; ++i)
				str += i;
			Debug.Log(str);
		}
	}
}