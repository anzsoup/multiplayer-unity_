using UnityEngine.Networking;
using System.Collections.Generic;

namespace LLenok.Networking
{
	/// <summary>
	/// This configuration must be shared between client and server, or nothing works.
	/// </summary>
	public static class NetworkConfig
	{
		private static ConnectionConfig _config;
		private static Dictionary<QosType, int> _channelDict;

		public static ConnectionConfig Config
		{
			get { return _config ?? (_config = LoadApplicationNetworkConfiguration()); }
		}

		public static Dictionary<QosType, int> Channels
		{
			get
			{
				if (_channelDict == null)
				{
					LoadApplicationNetworkConfiguration();
				}

				return _channelDict;
			}
		}

		public const QosType DefaultChannel = QosType.ReliableSequenced;

		private static ConnectionConfig LoadApplicationNetworkConfiguration()
		{
			var config = new ConnectionConfig();

			// Add a few network channels, which are common to all instances of the client and server.
			// Notice that it is meaningless to add multiple instances of the same channel type, as the
			// server will not be able to distinguish between them.
			_channelDict = new Dictionary<QosType, int>();
			_channelDict[QosType.ReliableSequenced] = config.AddChannel(QosType.ReliableSequenced);
			_channelDict[QosType.Unreliable] = config.AddChannel(QosType.Unreliable);

			return config;
		}
	}
}
