using UnityEngine;
using System.Collections;
using LLenok.Networking;
using Facepunch.Steamworks;

namespace LLenok.Steam
{
	public class SteamUser
	{
		public RMPPeer Peer { get; private set; }
		public ulong SteamId { get; private set; }
		public string Username { get; private set; }
		public Auth.Ticket SteamTicket { get; private set; }

		public SteamUser(RMPPeer peer, ulong steamId, string username)
		{
			// 본인의 유저객체일 경우 Session == null
			Peer = peer;
			SteamId = steamId;
			Username = username;
		}

		public Auth.Ticket GetAuthSessionTicket()
		{
			if (Client.Instance == null)
			{
				Debug.LogError("Failed to get steam ticket. Steam client not initialized.");
				return null;
			}

			SteamTicket = Client.Instance.Auth.GetAuthSessionTicket();
			return SteamTicket;
		}

		public void CancelAuthSessionTicket()
		{
			if (SteamTicket != null)
			{
				SteamTicket.Cancel();
				SteamTicket = null;
			}
		}
	}
}