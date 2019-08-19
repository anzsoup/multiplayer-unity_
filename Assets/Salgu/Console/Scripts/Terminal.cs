using UnityEngine;
using System.Text;
using UnityEngine.Assertions;
using UnityEditor;

namespace Salgu.Console
{
	public enum TerminalState
	{
		Close,
		OpenSmall,
		OpenFull
	}

	[System.Serializable]
	public enum TerminalConsoleColor
	{
		Black			= System.ConsoleColor.Black,
		DarkBlue		= System.ConsoleColor.DarkBlue,
		DarkGreen		= System.ConsoleColor.DarkGreen,
		DarkCyan		= System.ConsoleColor.DarkCyan,
		DarkRed			= System.ConsoleColor.DarkRed,
		DarkMagenta		= System.ConsoleColor.DarkMagenta,
		DarkYellow		= System.ConsoleColor.DarkYellow,
		Gray			= System.ConsoleColor.Gray,
		DarkGray		= System.ConsoleColor.DarkGray,
		Blue			= System.ConsoleColor.Blue,
		Green			= System.ConsoleColor.Green,
		Cyan			= System.ConsoleColor.Cyan,
		Red				= System.ConsoleColor.Red,
		Magenta			= System.ConsoleColor.Magenta,
		Yellow			= System.ConsoleColor.Yellow,
		White			= System.ConsoleColor.White
	}

	public class Terminal : MonoBehaviour
	{
		[Header("Window")]
		[Range(0, 1)]
		[SerializeField]
		float MaxHeight = 0.7f;

		[SerializeField]
		[Range(0, 1)]
		float SmallTerminalRatio = 0.33f;

		[Range(100, 1000)]
		[SerializeField]
		float ToggleSpeed = 360;

		[SerializeField] string ToggleHotkey		= "`";
		[SerializeField] string ToggleFullHotkey	= "#`";
		[SerializeField] int BufferSize				= 512;

		[Header("Input")]
		[SerializeField] Font ConsoleFont;
		[SerializeField] string InputCaret			= ">";
		[SerializeField] bool ShowGUIButtons		= false;
		[SerializeField] bool RightAlignButtons		= false;

		[Header("Theme")]
		[Range(0, 1)]
		[SerializeField] float InputContrast		= 0f;
		[Range(0, 1)]
		[SerializeField] float InputAlpha			= 0.5f;

		[SerializeField] Color BackgroundColor		= new Color(0f, 0f, 0f, 0.8f);
		[SerializeField] Color ForegroundColor		= Color.white;
		[SerializeField] Color ShellColor			= new Color(0.8f, 0.8f, 0.8f, 1f);
		[SerializeField] Color InputColor			= Color.green;
		[SerializeField] Color WarningColor			= Color.yellow;
		[SerializeField] Color ErrorColor			= Color.red;

		[Header("Batchmode")]
		[SerializeField] TerminalConsoleColor ForegroundConsoleColor	= TerminalConsoleColor.White;
		[SerializeField] TerminalConsoleColor ShellConsoleColor			= TerminalConsoleColor.Gray;
		[SerializeField] TerminalConsoleColor InputConsoleColor			= TerminalConsoleColor.Green;
		[SerializeField] TerminalConsoleColor WarningConsoleColor		= TerminalConsoleColor.Yellow;
		[SerializeField] TerminalConsoleColor ErrorConsoleColor			= TerminalConsoleColor.Red;

		TerminalState state;
		bool input_fix;
		bool move_cursor;
		bool initial_open; // Used to focus on TextField when console opens
		Rect window;
		float current_open_t;
		float open_target;
		float real_window_size;
		string command_text;
		string cached_command_text;
		static Vector2 scroll_position;
		GUIStyle window_style;
		GUIStyle label_style;
		GUIStyle input_style;
		Texture2D background_texture;
		Texture2D input_background_texture;

		WindowsConsole windows_console;
		WindowsConsoleInput windows_input;

		public static CommandLog Buffer { get; private set; }
		public static CommandShell Shell { get; private set; }
		public static CommandHistory History { get; private set; }
		public static CommandAutoComplete Autocomplete { get; private set; }
		public static bool IssuedError { get { return Shell.IssuedErrorMessage != null; } }
		public bool IsClosed { get { return state == TerminalState.Close && Mathf.Approximately(current_open_t, open_target); } }

#if UNITY_EDITOR
		[MenuItem("GameObject/Salgu/Terminal", priority = 30)]
		static void CreateGameObject()
		{
			var find = FindObjectOfType<Terminal>();
			if (find != null)
			{
				Debug.LogError("Terminal Object already exists.");
				return;
			}

			var go = new GameObject("Terminal", typeof(Terminal));
			Undo.RegisterCreatedObjectUndo(go, "Create Terminal");
		}
#endif
		public static void Print(string format, params object[] message)
		{
			Print(TerminalLogType.ShellMessage, format, message);
		}

		public static void Print(TerminalLogType type, string format, params object[] message)
		{
			Buffer.HandleLog(string.Format(format, message), type);
			scroll_position.y = int.MaxValue;
		}

		public void SetState(TerminalState new_state)
		{
			input_fix = true;
			cached_command_text = command_text;
			command_text = "";

			switch (new_state)
			{
				case TerminalState.Close:
					open_target = 0;
					break;
				case TerminalState.OpenSmall:
					open_target = Screen.height * MaxHeight * SmallTerminalRatio;
					if (current_open_t > open_target)
					{
						// Prevent resizing from OpenFull to OpenSmall if window y position
						// is greater than OpenSmall's target
						open_target = 0;
						state = TerminalState.Close;
						return;
					}
					real_window_size = open_target;
					scroll_position.y = int.MaxValue;
					break;
				case TerminalState.OpenFull:
				default:
					real_window_size = Screen.height * MaxHeight;
					open_target = real_window_size;
					break;
			}

			state = new_state;
		}

		public void ToggleState(TerminalState new_state)
		{
			if (state == new_state)
				SetState(TerminalState.Close);
			else
				SetState(new_state);
		}

		void OnEnable()
		{
			Buffer = new CommandLog(BufferSize);
			Shell = new CommandShell();
			History = new CommandHistory();
			Autocomplete = new CommandAutoComplete();
			
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			if (Application.isBatchMode)
			{
				windows_console = new WindowsConsole();
				windows_input = new WindowsConsoleInput((System.ConsoleColor)InputConsoleColor, InputCaret);

				windows_console.Initialize();
				windows_console.SetTitle("Rust Server");
				windows_input.OnInputText += OnInputTextBatchmode;
				Buffer.LogMessageReceived += HandleLogBatchmode;

				Debug.Log("Batchmode Console Started");
			}
#endif

			Application.logMessageReceived += HandleUnityLog;
		}

		void OnDisable()
		{
			Application.logMessageReceived -= HandleUnityLog;
			Buffer.LogMessageReceived -= HandleLogBatchmode;
		}

		void Start()
		{
			DontDestroyOnLoad(gameObject);

			if (ConsoleFont == null)
				ConsoleFont = Font.CreateDynamicFontFromOSFont("Consolas", 16);

			command_text = "";
			cached_command_text = command_text;
			Assert.AreNotEqual(ToggleHotkey.ToLower(), "return", "Return is not a valid ToggleHotkey");

			SetupWindow();
			SetupInput();
			SetupLabels();

			Shell.RegisterCommands();

			if (IssuedError)
				Print(TerminalLogType.Error, "Error: {0}", Shell.IssuedErrorMessage);

			foreach (var command in Shell.Commands)
				Autocomplete.Register(command.Key);
		}

		void OnGUI()
		{
			if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey)))
			{
				SetState(TerminalState.OpenSmall);
				initial_open = true;
			}
			else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey)))
			{
				SetState(TerminalState.OpenFull);
				initial_open = true;
			}

			if (ShowGUIButtons)
				DrawGUIButtons();

			if (IsClosed)
				return;

			HandleOpenness();
			window = GUILayout.Window(88, window, DrawConsole, "", window_style);
		}

		void Update()
		{
			if (windows_input != null)
				windows_input.Update();
		}

		void OnDestroy()
		{
			if (windows_console != null)
				windows_console.Shutdown();
		}

		void SetupWindow()
		{
			real_window_size = Screen.height * MaxHeight / 3;
			window = new Rect(0, current_open_t - real_window_size, Screen.width, real_window_size);
			
			// Set background color
			background_texture = new Texture2D(1, 1);
			background_texture.SetPixel(0, 0, BackgroundColor);
			background_texture.Apply();

			window_style = new GUIStyle();
			window_style.normal.background = background_texture;
			window_style.padding = new RectOffset(4, 4, 4, 4);
			window_style.normal.textColor = ForegroundColor;
			window_style.font = ConsoleFont;
		}

		void SetupLabels()
		{
			label_style = new GUIStyle();
			label_style.font = ConsoleFont;
			label_style.normal.textColor = ForegroundColor;
			label_style.wordWrap = true;
		}

		void SetupInput()
		{
			input_style = new GUIStyle();
			input_style.padding = new RectOffset(4, 4, 4, 4);
			input_style.font = ConsoleFont;
			input_style.fixedHeight = ConsoleFont.fontSize * 1.6f;
			input_style.normal.textColor = InputColor;

			var dark_background = new Color();
			dark_background.r = BackgroundColor.r - InputContrast;
			dark_background.g = BackgroundColor.g - InputContrast;
			dark_background.b = BackgroundColor.b - InputContrast;
			dark_background.a = InputAlpha;

			input_background_texture = new Texture2D(1, 1);
			input_background_texture.SetPixel(0, 0, dark_background);
			input_background_texture.Apply();
			input_style.normal.background = input_background_texture;
		}

		void DrawConsole(int Window2D)
		{
			GUILayout.BeginVertical();

			scroll_position = GUILayout.BeginScrollView(scroll_position, false, false, GUIStyle.none, GUIStyle.none);
			GUILayout.FlexibleSpace();
			DrawLogs();
			GUILayout.EndScrollView();

			if (move_cursor)
			{
				CursorToEnd();
				move_cursor = false;
			}

			if (Event.current.Equals(Event.KeyboardEvent("escape")))
			{
				SetState(TerminalState.Close);
			}
			else if (Event.current.Equals(Event.KeyboardEvent("return"))
				|| Event.current.Equals(Event.KeyboardEvent("[enter]")))
			{
				EnterCommand();
			}
			else if (Event.current.Equals(Event.KeyboardEvent("up")))
			{
				command_text = History.Previous();
				move_cursor = true;
			}
			else if (Event.current.Equals(Event.KeyboardEvent("down")))
			{
				command_text = History.Next();
			}
			else if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey)))
			{
				ToggleState(TerminalState.OpenSmall);
			}
			else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey)))
			{
				ToggleState(TerminalState.OpenFull);
			}
			else if (Event.current.Equals(Event.KeyboardEvent("tab")))
			{
				CompleteCommand();
				move_cursor = true; // Wait till next draw call
			}

			GUILayout.BeginHorizontal();

			if (InputCaret != "")
				GUILayout.Label(InputCaret, input_style, GUILayout.Width(ConsoleFont.fontSize));

			GUI.SetNextControlName("command_text_field");
			command_text = GUILayout.TextField(command_text, input_style);

			if (input_fix && command_text.Length > 0)
			{
				command_text = cached_command_text; // Otherwise the TextField picks up the ToggleHotkey character event
				input_fix = false;                  // Prevents checking string Length every draw call
			}

			if (initial_open)
			{
				GUI.FocusControl("command_text_field");
				initial_open = false;
			}

			if (ShowGUIButtons && GUILayout.Button("| run", input_style, GUILayout.Width(Screen.width / 10)))
				EnterCommand();

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		void DrawLogs()
		{
			foreach (var log in Buffer.Logs)
			{
				label_style.normal.textColor = GetLogColor(log.type);
				GUILayout.Label(log.message, label_style);
			}
			GUILayout.Space(12);
		}

		void DrawGUIButtons()
		{
			int size = ConsoleFont.fontSize;
			float x_position = RightAlignButtons ? Screen.width - 7 * size : 0;

			// 7 is the number of chars in the button plus some padding, 2 is the line height.
			// The layout will resize according to the font size.
			GUILayout.BeginArea(new Rect(x_position, current_open_t, 7 * size, size * 2));
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Small", window_style))
				ToggleState(TerminalState.OpenSmall);
			else if (GUILayout.Button("Full", window_style))
				ToggleState(TerminalState.OpenFull);

			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		void HandleOpenness()
		{
			float dt = ToggleSpeed * Time.unscaledDeltaTime;

			if (current_open_t < open_target)
			{
				current_open_t += dt;
				if (current_open_t > open_target) current_open_t = open_target;
			}
			else if (current_open_t > open_target)
			{
				current_open_t -= dt;
				if (current_open_t < open_target) current_open_t = open_target;
			}
			else
			{
				if (input_fix)
				{
					input_fix = false;
				}
				return; // Already at target
			}

			window = new Rect(0, current_open_t - real_window_size, Screen.width, real_window_size);
		}

		void EnterCommand()
		{
			Print(TerminalLogType.Input, "{0} {1}", InputCaret, command_text);

			string copy = command_text;

			command_text = "";
			scroll_position.y = int.MaxValue;
			
			RunCommand(copy);
		}

		void OnInputTextBatchmode(string obj)
		{
			RunCommand(obj);
		}

		/// <summary>
		/// All you have to do is just call this method to run your command.
		/// </summary>
		public static void RunCommand(string command)
		{
			Shell.RunCommand(command);
			History.Push(command);
		}

		void CompleteCommand()
		{
			string head_text = command_text;
			int format_width = 0;

			string[] completion_buffer = Autocomplete.Complete(ref head_text, ref format_width);
			int completion_length = completion_buffer.Length;

			if (completion_length != 0)
				command_text = head_text;

			if (completion_length > 1)
			{
				// Print possible completions
				var log_buffer = new StringBuilder();

				foreach (string completion in completion_buffer)
					log_buffer.Append(completion.PadRight(format_width + 4));

				Print("{0}", log_buffer);
				scroll_position.y = int.MaxValue;
			}
		}

		void CursorToEnd()
		{
			// Get editor state everytime, or it won't work correctly.
			var editor_state = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			editor_state.MoveCursorToPosition(new Vector2(999, 999));
		}

		void HandleUnityLog(string message, string stack_trace, LogType type)
		{
			Buffer.HandleLog(message, stack_trace, (TerminalLogType)type);
			scroll_position.y = int.MaxValue;
		}

		void HandleLogBatchmode(LogItem log)
		{
			if (log.type == TerminalLogType.Warning)
				System.Console.ForegroundColor = (System.ConsoleColor)WarningConsoleColor;
			else if (log.type == TerminalLogType.Error)
				System.Console.ForegroundColor = (System.ConsoleColor)ErrorConsoleColor;
			else if (log.type == TerminalLogType.ShellMessage)
				System.Console.ForegroundColor = (System.ConsoleColor)ShellConsoleColor;
			else
				System.Console.ForegroundColor = (System.ConsoleColor)ForegroundConsoleColor;

			// We're half way through typing something, so clear this line ..
			if (System.Console.CursorLeft != 0)
				windows_input.ClearLine();

			System.Console.WriteLine(log.message);

			// If we were typing something re-add it.
			windows_input.RedrawInputLine();
		}

		Color GetLogColor(TerminalLogType type)
		{
			switch (type)
			{
				case TerminalLogType.Log: return ForegroundColor;
				case TerminalLogType.Warning: return WarningColor;
				case TerminalLogType.Input: return InputColor;
				case TerminalLogType.ShellMessage: return ShellColor;
				default: return ErrorColor;
			}
		}
	}
}
