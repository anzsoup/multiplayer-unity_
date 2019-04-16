using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChickenIngot.Console
{
	public enum TerminalLogType
	{
		Error     = LogType.Error,
		Assert    = LogType.Assert,
		Warning   = LogType.Warning,
		Log		  = LogType.Log,
		Exception = LogType.Exception,
		Input,
		ShellMessage
	}

	public struct LogItem
	{
		public TerminalLogType type;
		public string message;
		public string stack_trace;
	}

	public class CommandLog
	{
		List<LogItem> logs = new List<LogItem>();
		int max_items;

		public List<LogItem> Logs { get { return logs; } }
		public Action<LogItem> LogMessageReceived { get; set; }

		public CommandLog(int max_items)
		{
			this.max_items = max_items;
		}

		public void HandleLog(string message, TerminalLogType type)
		{
			HandleLog(message, "", type);
		}

		public void HandleLog(string message, string stack_trace, TerminalLogType type)
		{
			LogItem log = new LogItem()
			{
				message = message,
				stack_trace = stack_trace,
				type = type
			};

			logs.Add(log);

			if (logs.Count > max_items)
				logs.RemoveAt(0);

			if (LogMessageReceived != null)
				LogMessageReceived.Invoke(log);
		}

		public void Clear()
		{
			logs.Clear();
		}
	}
}
