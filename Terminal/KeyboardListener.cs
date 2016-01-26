using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Terminal
{
	/// <summary>
	/// Listens keyboard globally.
	/// 
	/// <remarks>Uses WH_KEYBOARD_LL.</remarks>
	/// </summary>
	public class KeyboardListener : IDisposable
	{
		/// <summary>
		/// Raw keyevent handler.
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="args">raw keyevent arguments</param>
		public delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args);
		
		/// <summary>
		/// Asynchronous callback hook.
		/// </summary>
		/// <param name="character">Character</param>
		/// <param name="keyEvent">Keyboard event</param>
		/// <param name="vkCode">VKCode</param>
		private delegate void KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode, string character);

		#region Events

		/// <summary>
		/// Fired when any of the keys is pressed down.
		/// </summary>
		public event RawKeyEventHandler KeyDown;

		/// <summary>
		/// Fired when any of the keys is released.
		/// </summary>
		public event RawKeyEventHandler KeyUp;

		#endregion

		#region Fields

		private Dispatcher _dispatcher;

		/// <summary>
		/// Hook ID
		/// </summary>
		private IntPtr _hookId = IntPtr.Zero;

		/// <summary>
		/// Event to be invoked asynchronously (BeginInvoke) each time key is pressed.
		/// </summary>
		private KeyboardCallbackAsync _hookedKeyboardCallbackAsync;

		/// <summary>
		/// Contains the hooked callback in runtime.
		/// </summary>
		private InterceptKeys.LowLevelKeyboardProc _hookedLowLevelKeyboardProc;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates global keyboard listener.
		/// </summary>
		public KeyboardListener()
		{
			// Dispatcher thread handling the KeyDown/KeyUp events.
			_dispatcher = Dispatcher.CurrentDispatcher;

			// We have to store the LowLevelKeyboardProc, so that it is not garbage collected runtime.
			_hookedLowLevelKeyboardProc = LowLevelKeyboardProc;

			// Set the hook.
			_hookId = InterceptKeys.SetHook(_hookedLowLevelKeyboardProc);

			// Assign the asynchronous callback event
			_hookedKeyboardCallbackAsync = new KeyboardCallbackAsync(KeyboardListener_KeyboardCallbackAsync);
		}

		/// <summary>
		/// Destroys global keyboard listener.
		/// </summary>
		~KeyboardListener()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Disposes the hook.
		/// <remarks>This call is required as it calls the UnhookWindowsHookEx.</remarks>
		/// </summary>
		public void Dispose()
		{
			InterceptKeys.UnhookWindowsHookEx(_hookId);
		}

		/// <summary>
		/// Actual callback hook.
		/// </summary>
		/// <remarks>Calls asynchronously the asyncCallback.</remarks>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
		{
			var chars = string.Empty;

			if (nCode >= 0)
			{
				if ((wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYDOWN) ||
					(wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYUP) ||
					(wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYDOWN) ||
					(wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYUP))
				{
					// Captures the character(s) pressed only on WM_KEYDOWN
					chars = InterceptKeys.VKCodeToString((uint)Marshal.ReadInt32(lParam),
						((wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYDOWN) ||
						 (wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYDOWN)));

					_hookedKeyboardCallbackAsync.BeginInvoke((InterceptKeys.KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), chars, null, null);
				}
			}

			return InterceptKeys.CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// HookCallbackAsync procedure that calls accordingly the KeyDown or KeyUp events.
		/// </summary>
		/// <param name="keyEvent">Keyboard event</param>
		/// <param name="vkCode">VKCode</param>
		/// <param name="character">Character as string.</param>
		private void KeyboardListener_KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode, string character)
		{
			switch (keyEvent)
			{
				// KeyDown events
				case InterceptKeys.KeyEvent.WM_KEYDOWN:
					if (KeyDown != null)
					{
						_dispatcher.BeginInvoke(new RawKeyEventHandler(KeyDown), this, new RawKeyEventArgs(vkCode, false, character));
					}
					break;
				case InterceptKeys.KeyEvent.WM_SYSKEYDOWN:
					if (KeyDown != null)
					{
						_dispatcher.BeginInvoke(new RawKeyEventHandler(KeyDown), this, new RawKeyEventArgs(vkCode, true, character));
					}
					break;

				// KeyUp events
				case InterceptKeys.KeyEvent.WM_KEYUP:
					if (KeyUp != null)
					{
						_dispatcher.BeginInvoke(new RawKeyEventHandler(KeyUp), this, new RawKeyEventArgs(vkCode, false, character));
					}
					break;
				case InterceptKeys.KeyEvent.WM_SYSKEYUP:
					if (KeyUp != null)
					{
						_dispatcher.BeginInvoke(new RawKeyEventHandler(KeyUp), this, new RawKeyEventArgs(vkCode, true, character));
					}
					break;

				default:
					break;
			}
		}

		#endregion
	}
}
