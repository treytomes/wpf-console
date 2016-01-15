using System;

namespace Terminal
{
	/// <summary>
	/// Simulates the Console.Read*/Write* functionality, to read ASCII characters from the keyboard.
	/// </summary>
	public interface IConsole
	{
		void Write(char ch);
		void Write(string text, params object[] args);
		void WriteLine(string text, params object[] args);
		bool Read(Action<char> receiver);
		bool ReadLine(Action<string> receiver);
	}
}