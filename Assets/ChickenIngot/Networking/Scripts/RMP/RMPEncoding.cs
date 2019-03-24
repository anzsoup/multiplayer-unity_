using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace ChickenIngot.Networking
{
	// 미완성
	[AttributeUsage(AttributeTargets.Method)]
	public class RMPEncodingRuleAttribute : Attribute
	{
		public Type Type { get; private set; }

		public RMPEncodingRuleAttribute(Type type)
		{
			Type = type;
		}
	}

	public static class RMPEncoding
	{
		public enum ProtocolId : byte
		{
			RPC,
			Replicate,
			Remove,
		}

		public enum ParameterType : byte
		{
			None,
			Bool,
			Int,
			Float,
			String,
			ByteArray,
			Vector2,
			Vector3,
			Quaternion,
			Vector2Int,
			Vector3Int,
		}

		private static Dictionary<string, Action> _rules;

		public static void RegisterEncodingRules()
		{
			_rules = new Dictionary<string, Action>();
			var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			
			var types = Assembly.GetExecutingAssembly().GetTypes();
			foreach (var type in types)
			{
				var methods = type.GetMethods(flags);
				foreach (var method in methods)
				{
					var attribute = Attribute.GetCustomAttribute(
						method, typeof(RMPEncodingRuleAttribute)) as RMPEncodingRuleAttribute;

					if (attribute == null)
						continue;

					var key = attribute.Type.ToString();
					if (_rules.ContainsKey(key))
					{
						Debug.LogWarning(string.Format("Multiple definition of RMP encoding rule. : {0}", key));
						continue;
					}

					var proc = Delegate.CreateDelegate(typeof(Action), method) as Action;
					_rules.Add(key, proc);
				}
			}
		}

		public static void PushParameter(Packet msg, object parameter)
		{
			if (parameter == null) msg.PushByte((byte)ParameterType.None);
			else if (parameter is int)
			{
				msg.PushByte((byte)ParameterType.Int);
				msg.PushInt32((int)parameter);
			}
			else if (parameter is float)
			{
				msg.PushByte((byte)ParameterType.Float);
				msg.PushSingle((float)parameter);
			}
			else if (parameter is bool)
			{
				msg.PushByte((byte)ParameterType.Bool);
				msg.PushBool((bool)parameter);
			}
			else if (parameter is string)
			{
				msg.PushByte((byte)ParameterType.String);
				msg.PushString((string)parameter);
			}
			else if (parameter is byte[])
			{
				msg.PushByte((byte)ParameterType.ByteArray);
				byte[] param = (byte[])parameter;
				msg.PushByteArray(param);
			}
			else if (parameter is Vector2)
			{
				msg.PushByte((byte)ParameterType.Vector2);
				msg.PushVector2((Vector2)parameter);
			}
			else if (parameter is Vector3)
			{
				msg.PushByte((byte)ParameterType.Vector3);
				msg.PushVector3((Vector3)parameter);
			}
			else if (parameter is Quaternion)
			{
				msg.PushByte((byte)ParameterType.Quaternion);
				msg.PushQuaternion((Quaternion)parameter);
			}
			else if (parameter is Vector2Int)
			{
				msg.PushByte((byte)ParameterType.Vector2Int);
				msg.PushVector2Int((Vector2Int)parameter);
			}
			else if (parameter is Vector3Int)
			{
				msg.PushByte((byte)ParameterType.Vector3Int);
				msg.PushVector3Int((Vector3Int)parameter);
			}
		}

		public static object PopParameter(Packet msg)
		{
			ParameterType paramType = (ParameterType)msg.PopByte();
			object parameter = null;
			switch (paramType)
			{
				case ParameterType.None:
					// object[]가 아닌 null 값을 반환해야한다
					parameter = null;
					break;

				case ParameterType.Int:
					parameter = msg.PopInt32();
					break;

				case ParameterType.Float:
					parameter = msg.PopSingle();
					break;

				case ParameterType.Bool:
					parameter = msg.PopBool();
					break;

				case ParameterType.String:
					parameter = msg.PopString();
					break;

				case ParameterType.ByteArray:
					parameter = msg.PopByteArray();
					break;

				case ParameterType.Vector2:
					parameter = msg.PopVector2();
					break;

				case ParameterType.Vector3:
					parameter = msg.PopVector3();
					break;

				case ParameterType.Quaternion:
					parameter = msg.PopQuaternion();
					break;

				case ParameterType.Vector2Int:
					parameter = msg.PopVector2Int();
					break;

				case ParameterType.Vector3Int:
					parameter = msg.PopVector3Int();
					break;
			}

			return parameter;
		}
	}
}