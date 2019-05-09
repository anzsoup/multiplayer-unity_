using System.Text;
using System.Diagnostics;

namespace LLenok.Console
{
	public static class BuiltinCommands
	{
		[ConsoleCommand(Help = "Clear the command console", MaxArgCount = 0)]
		static void CommandClear(CommandArg[] args)
		{
			Terminal.Buffer.Clear();
		}
		
		[ConsoleCommand(Help = "Display help information about a command", MaxArgCount = 1)]
		static void CommandHelp(CommandArg[] args)
		{
			if (args.Length == 0)
			{
				foreach (var command in Terminal.Shell.Commands)
				{
					Terminal.Print("{0}: {1}", command.Key.PadRight(16), command.Value.help);
				}
				return;
			}

			string command_name = args[0].String.ToLower();

			if (!Terminal.Shell.Commands.ContainsKey(command_name))
			{
				Terminal.Shell.IssueErrorMessage("Command {0} could not be found.", command_name);
				return;
			}

			var info = Terminal.Shell.Commands[command_name];

			if (info.help == null)
				Terminal.Print("{0} does not provide any help documentation.", command_name);
			else if (info.hint == null)
				Terminal.Print(info.help);
			else
				Terminal.Print("{0}\nUsage: {1}", info.help, info.hint);
		}

		[ConsoleCommand(Help = "Time the execution of a command", MinArgCount = 1)]
		static void CommandTime(CommandArg[] args)
		{
			var sw = new Stopwatch();
			sw.Start();

			Terminal.Shell.RunCommand(JoinArguments(args));

			sw.Stop();
			Terminal.Print("Time: {0}ms", (double)sw.ElapsedTicks / 10000);
		}

		[ConsoleCommand(Help = "Output message")]
		static void CommandPrint(CommandArg[] args)
		{
			Terminal.Print(JoinArguments(args));
		}

	#if DEBUG
		[ConsoleCommand(Help = "Output the stack trace of the previous message", MaxArgCount = 0)]
		static void CommandTrace(CommandArg[] args)
		{
			int log_count = Terminal.Buffer.Logs.Count;

			if (log_count - 2 < 0)
			{
				Terminal.Print("Nothing to trace.");
				return;
			}

			var log_item = Terminal.Buffer.Logs[log_count - 2];

			if (log_item.stack_trace == "")
				Terminal.Print("{0} (no trace)", log_item.message);
			else
				Terminal.Print(log_item.stack_trace);
		}
	#endif

		[ConsoleCommand(Help = "List all variables or set a variable value")]
		static void CommandSet(CommandArg[] args)
		{
			if (args.Length == 0)
			{
				foreach (var kv in Terminal.Shell.Variables)
				{
					Terminal.Print("{0}: {1}", kv.Key.PadRight(16), kv.Value);
				}
				return;
			}

			string variable_name = args[0].String;

			if (variable_name[0] == '$')
				Terminal.Print(TerminalLogType.Warning, "Warning: Variable name starts with '$', '${0}'.", variable_name);

			Terminal.Shell.SetVariable(variable_name, JoinArguments(args, 1));
		}

		[ConsoleCommand(Help = "No operation")]
		static void CommandNoop(CommandArg[] args) { }

		[ConsoleCommand(Help = "Quit running application", MaxArgCount = 0)]
		static void CommandQuit(CommandArg[] args)
		{
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			UnityEngine.Application.Quit();
		#endif
		}

		static string JoinArguments(CommandArg[] args, int start = 0)
		{
			var sb = new StringBuilder();
			int arg_length = args.Length;

			for (int i = start; i < arg_length; i++)
			{
				sb.Append(args[i].String);

				if (i < arg_length - 1)
					sb.Append(" ");
			}

			return sb.ToString();
		}
	}
}
