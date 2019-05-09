using UnityEngine;

namespace LLenok.Networking.Demo
{
	/// -----------------------------------------------------------------------------------
	/// [RCP Message]
	///		스크립트가 RPC 메시지와 기타 이벤트를 받으려면
	///		RMPNetworkView 의 'Message Receivers' 에 등록되어 있어야 한다.
	/// -----------------------------------------------------------------------------------

	public class DemoChatting : MonoBehaviour
	{
		[SerializeField]
		private RMPNetworkView _view;

		public void SendChat(string msg)
		{
			_view.RPC(RPCOption.ToServer, "svRPC_Chat", msg);
		}

		// 서버가 ToServer 옵션을 사용하면 자신의 메소드가 호출된다.
		// 덕분에 코드를 일관적으로 작성할 수 있다.
		// SendChat 메소드를 보면 굳이 자신이 서버인지 클라이언트인지 확인하지 않았다.
		[RMP]
		[ServerOnly]
		private void svRPC_Chat(string msg)
		{
			// 메소드가 어느 클라이언트에 의해 호출되었는지 확인하려면 RMPNetworkView.MessageSender 를 사용한다.
			// 서버 자신이 호출한 경우에는 null 이다.
			if (_view.MessageSender == null)
				msg = "Server : " + msg;
			else
				msg = "Client : " + msg;

			clRPC_Chat(msg);
			_view.RPC(RPCOption.Broadcast, "clRPC_Chat", msg);
		}

		// 사실 어트리뷰트들은 아무런 기능도 하지 않는다. 하지만 가독성을 위해 붙이는 것이 좋다.
		[RMP]
		[ClientOnly]
		private void clRPC_Chat(string msg)
		{
			Debug.Log(msg);
		}
	}
}