using System;
using System.ComponentModel;
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

		#endregion
		
		#region Constructors

		public MainWindow()
		{
			InitializeComponent();

			console.ForegroundColor = Colors.White;
			console.WriteLine("\x0002 Hello, world! \x0002");
			console.WriteLine("Welcome to the WPF Console!");
			console.WriteLine();

			console.BackgroundColor = Colors.Red;
			console.WriteLine("This is a test.");

			_lineNumber = 0;

			console.ForegroundColor = Colors.Gray;
			console.BackgroundColor = Colors.Black;

			if (!DesignerProperties.GetIsInDesignMode(this))
			{
				Task.Run(REPL_Callback);
			}
		}

		#endregion

		#region Event Handlers

		private void Timer_Callback(object sender, EventArgs e)
		{
			console.WriteLine("{0}. *****", _lineNumber++);
		}

		private async Task REPL_Callback()
		{
			while (true)
			{
				Dispatcher.Invoke(() => console.Write("> "));
				var text = await console.ReadLine();
				 
				if (text.StartsWith("ECHO ", StringComparison.CurrentCultureIgnoreCase))
				{
					MessageBox.Show(text.Substring(5));
				}
				else if (string.Compare(text, "GO", true) == 0)
				{
					Dispatcher.Invoke(() =>
					{
						console.ForegroundColor = Colors.BlanchedAlmond;
						console.BackgroundColor = Colors.ForestGreen;
						console.WriteLine("This is another test.");

						_timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / TARGET_FPS), DispatcherPriority.Normal, Timer_Callback, Dispatcher);
						_timer.Start();
					});
					break;
				}
			}
		}

		//private void _listener_KeyDown(object sender, RawKeyEventArgs args)
		//{
		//	console.Write(args.Character);
		//}

		#endregion
	}
}
