using System;
using System.Windows.Input;

namespace Terminal
{
	/// <summary>
	/// Raw KeyEvent arguments.
	/// </summary>
	public class RawKeyEventArgs : EventArgs
	{
		/// <summary>
		/// VKCode of the key.
		/// </summary>
		public readonly int VKCode;

		/// <summary>
		/// WPF Key of the key.
		/// </summary>
		public readonly Key Key;

		/// <summary>
		/// Is the hitted key system key.
		/// </summary>
		public readonly bool IsSysKey;

		/// <summary>
		/// Unicode character of key pressed.
		/// </summary>
		public readonly string Character;

		/// <summary>
		/// Create raw keyevent arguments.
		/// </summary>
		public RawKeyEventArgs(int vkCode, bool isSysKey, string character)
		{
			VKCode = vkCode;
			IsSysKey = isSysKey;
			Character = character;
			Key = KeyInterop.KeyFromVirtualKey(vkCode);
		}

		/// <summary>
		/// Convert to string.
		/// </summary>
		/// <returns>Returns string representation of this key, if not possible empty string is returned.</returns>
		public override string ToString()
		{
			return Character;
		}
	}
}