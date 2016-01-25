using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Terminal
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Constants

		private const int TARGET_FPS = 120;

		#endregion

		#region Fields

		private DispatcherTimer _timer;
		private int _lineNumber;

		private bool _ready;
		private ManualResetEvent _readySwitch;

		#endregion
		
		#region Constructors

		public MainWindow()
		{
			InitializeComponent();

			console.WriteLine("\x0002 Hello, world! \x0002");
			console.WriteLine(string.Empty);
			console.ForegroundColor = Colors.White;
			console.BackgroundColor = Colors.Red;
			console.WriteLine("This is a test.");

			_lineNumber = 0;

			console.Write("> ");

			_readySwitch = new ManualResetEvent(false);
			console.ReadLine()
				.ContinueWith(text =>
				{
					MessageBox.Show(text.Result.ToString());
					_readySwitch.Set();
				});

			Task.Run(() =>
			{
				_readySwitch.WaitOne(); // wait until the read is finished

				Dispatcher.Invoke(() =>
				{
					console.BackgroundColor = Colors.DarkGreen;
					console.WriteLine("This is another test.");

					_timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / TARGET_FPS), DispatcherPriority.Normal, Timer_Callback, Dispatcher);
					_timer.Start();
				});
			});

			//_listener = new KeyboardListener();
			//_listener.KeyDown += _listener_KeyDown;
		}

		#endregion

		#region Event Handlers

		private void Timer_Callback(object sender, EventArgs e)
		{
			console.WriteLine("{0}. *****", _lineNumber++);
		}

		//private void _listener_KeyDown(object sender, RawKeyEventArgs args)
		//{
		//	console.Write(args.Character);
		//}

		#endregion
	}
}
