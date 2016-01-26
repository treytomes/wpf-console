# wpf-console
WPF-based Extended ASCII console.

When built, Terminal.exe can be used as a class library.  The primary class is ConsoleCanvas, which is a pure WPF UI class.  No DirectX or other graphics acceleration going on here.  Add the ConsoleCanvas somewhere in your XAML code:

<local:ConsoleCanvas x:Name="console" />

Add some code-behind to render initial text to the console and start a REPL loop:

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

Starting the REPL loop is optional, but you probably don't want it to run in design mode.

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
			}
		}

