using System;
using System.Diagnostics;
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
			console.BackgroundColor = Colors.DarkGreen;
			console.WriteLine("This is another test.");

			_lineNumber = 0;

			_timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / TARGET_FPS), DispatcherPriority.Normal, Timer_Callback, Dispatcher);
			_timer.Start();
		}

		#endregion

		#region Event Handlers

		private void Timer_Callback(object sender, EventArgs e)
		{
			console.WriteLine("{0}. *****", _lineNumber++);
		}

		#endregion
	}
}
