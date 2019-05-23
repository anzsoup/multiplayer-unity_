using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Salgu.Networking
{
	public sealed class MessageCache
	{
		private readonly Dictionary<string, MethodInfo>[] _cache;
		private MonoBehaviour[] _messageReceivers;

		public MessageCache(MonoBehaviour[] messageReceivers)
		{
			_messageReceivers = messageReceivers;
			_cache = new Dictionary<string, MethodInfo>[messageReceivers.Length];
			for (int i = 0; i < messageReceivers.Length; ++i)
			{
				_cache[i] = new Dictionary<string, MethodInfo>();
			}
		}

		public MethodInfo Access(int address, string message)
		{
			if (address < 0 || address >= _cache.Length)
			{
				throw new Exception("Address out of range.");
			}

			Dictionary<string, MethodInfo> dict = _cache[address];
			MethodInfo method = null;
			if (!dict.TryGetValue(message, out method))
			{
				method = CacheMiss(address, message);
			}

			return method;
		}

		private MethodInfo CacheMiss(int address, string message)
		{
			MonoBehaviour receiver = _messageReceivers[address];
			MethodInfo method = null;

			if (receiver != null)
			{
				method = receiver.GetType().GetMethod(message, BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.Instance);
			}

			// receiver 가 null 이거나 method 가 null 이어도 캐시에 기록한다.
			// 리플렉션 연산을 두 번 이상 하지 않는 것이 목적이기 때문.
			_cache[address].Add(message, method);
			return method;
		}
	}
}
