using System.Collections.Generic;

namespace Soysaeu.Networking
{
	public class RMPPeerClient : RMPPeer
	{
		public override void OnCreated(int hostId, int connectionId)
		{
			base.OnCreated(hostId, connectionId);

			if (ClientPeers == null)
				ClientPeers = new Dictionary<int, RMPPeer>();
			ClientPeers.Add(connectionId, this);

			RMPNetworkService.OnClientConnect.Invoke(this);
		}

		public override void OnRemoved()
		{
			base.OnRemoved();

			if (ClientPeers != null)
				ClientPeers.Remove(ConnectionId);

			RMPNetworkService.OnClientDisconnect.Invoke(this);
		}
	}
}