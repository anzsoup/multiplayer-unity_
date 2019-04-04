using System;

namespace ChickenIngot.Console
{
	public class WindowsConsoleInput
	{
		ConsoleColor inputColor;
		string inputCaret;

		//public delegate void InputText( string strInput );
		public event Action<string> OnInputText;
		public string inputString;

		public WindowsConsoleInput(ConsoleColor inputColor, string inputCaret)
		{
			this.inputColor = inputColor;
			this.inputCaret = string.IsNullOrEmpty(inputCaret) ? ">" : inputCaret;
		}

		public void ClearLine()
		{
			System.Console.CursorLeft = 0;
			System.Console.Write(new string(' ', System.Console.BufferWidth));
			System.Console.CursorTop--;
			System.Console.CursorLeft = 0;
		}

		public void RedrawInputLine()
		{
			if (System.Console.CursorLeft > 0)
				ClearLine();

			System.Console.ForegroundColor = inputColor;
			System.Console.Write(inputString);
		}

		internal void OnBackspace()
		{
			if (string.IsNullOrEmpty(inputString)) return;

			inputString = inputString.Substring(0, inputString.Length - 1);
			RedrawInputLine();
		}

		internal void OnEscape()
		{
			ClearLine();
			inputString = "";
		}

		internal void OnEnter()
		{
			if (string.IsNullOrEmpty(inputString)) return;

			ClearLine();
			System.Console.ForegroundColor = inputColor;
			System.Console.WriteLine(string.Format("{0} {1}", inputCaret, inputString));

			var strtext = inputString;
			inputString = "";

			if (OnInputText != null)
			{
				OnInputText(strtext);
			}
		}

		public void Update()
		{
			if (!System.Console.KeyAvailable) return;
			var key = System.Console.ReadKey();

			if (key.Key == ConsoleKey.Enter)
			{
				OnEnter();
				return;
			}

			if (key.Key == ConsoleKey.Backspace)
			{
				OnBackspace();
				return;
			}

			if (key.Key == ConsoleKey.Escape)
			{
				OnEscape();
				return;
			}

			if (key.KeyChar != '\u0000')
			{
				inputString += key.KeyChar;
				RedrawInputLine();
				return;
			}
		}
	}
}