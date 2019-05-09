
namespace LLenok.Networking
{
	public class RMPPeerServer : RMPPeer
	{
		public override void OnCreated(int hostId, int connectionId)
		{
			base.OnCreated(hostId, connectionId);
			ServerPeer = this;
			RMPNetworkService.OnConnectToServer.Invoke(this);
		}

		public override void OnRemoved()
		{
			base.OnRemoved();

			ServerPeer = null;
			RMPNetworkService.OnDisconnectFromServer.Invoke(this);
		}
	}
}