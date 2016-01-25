using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Terminal
{
	public class ConsoleCanvas : Canvas, IConsole
	{
		private struct ConsoleAttribute
		{
			public SolidColorBrush ForegroundColor;
			public SolidColorBrush BackgroundColor;
			public char Character;
		}

		private struct ConsolePosition
		{
			public ConsolePosition(int row, int column)
			{
				Row = row;
				Column = column;
			}

			public int Row;
			public int Column;
		}

		#region Constants

		private const int TILESET_WIDTH = 8;
		private const int TILESET_HEIGHT = 8;
		private const int TAB_WIDTH = 4;
		private const int CURSOR_BLINK_MS = 300;
		private const char CURSOR_CHAR = '_';

		private const int DEFAULT_ROWS = 25;
		private const int DEFAULT_COLUMNS = 80;

		#endregion

		#region Fields

		private TileSet _asciiTiles;
		private int _cursorRow;
		private int _cursorColumn;
		private ConsoleAttribute[,] _buffer;

		private List<ConsolePosition> _redrawList;
		private Size _tileSize;
		private RenderTargetBitmap _backBuffer;

		private KeyboardListener _keyboard;
		private bool _isReading;
		private string _readBuffer;
		
		#endregion

		#region Constructors

		public ConsoleCanvas()
			: this(DEFAULT_ROWS, DEFAULT_COLUMNS)
		{
		}

		public ConsoleCanvas(int rows, int columns)
		{
			Background = Brushes.Black;

			_asciiTiles = new TileSet(Properties.Resources.OEM437_8, 8, 8);
			Rows = rows;
			Columns = columns;

			Width = Columns * _asciiTiles.TileWidth;
			Height = Rows * _asciiTiles.TileHeight;

			_cursorRow = 0;
			_cursorColumn = 0;
			_tileSize = new Size(_asciiTiles.TileWidth, _asciiTiles.TileHeight);
			_buffer = new ConsoleAttribute[Rows, Columns];
			_backBuffer = new RenderTargetBitmap((int)Width, (int)Height, _asciiTiles._source.DpiX, _asciiTiles._source.DpiY, PixelFormats.Default);
			_redrawList = new List<ConsolePosition>();

			_keyboard = new KeyboardListener();
			_keyboard.KeyDown += Keyboard_KeyDown;
			_keyboard.KeyUp += Keyboard_KeyUp;
			_isReading = false;
			_readBuffer = string.Empty;

			ForegroundColor = Colors.Gray;
			BackgroundColor = Colors.Black;
			ScrollAtBottom = true;
		}

		#endregion

		#region Properties

		public Color ForegroundColor { get; set; }

		public Color BackgroundColor { get; set; }

		public int Rows { get; private set; }

		public int Columns { get; private set; }

		public int CursorRow
		{
			get
			{
				return _cursorRow;
			}
			set
			{
				_cursorRow = value % Rows;
			}
		}

		public int CursorColumn
		{
			get
			{
				return _cursorColumn;
			}
			set
			{
				_cursorColumn = value % Columns;
			}
		}

		/// <summary>
		/// Should the console scroll everything up one line when it gets 
		/// </summary>
		public bool ScrollAtBottom { get; set; }

		#endregion

		#region Methods

		public Task<char> Read()
		{
			if (_isReading)
			{
				throw new InvalidOperationException("The buffer is already locked for reading.");
			}
			_isReading = true;
			_readBuffer = string.Empty;

			return Task.Run(() =>
			{
				while (_readBuffer.Length == 0) ;

				var result = _readBuffer[0];
				_isReading = false;
				_readBuffer = string.Empty;
				return result;
			});
		}

		public Task<string> ReadLine()
		{
			if (_isReading)
			{
				throw new InvalidOperationException("The buffer is already locked for reading.");
			}
			_isReading = true;
			_readBuffer = string.Empty;

			return Task.Run(() =>
			{
				while (!_readBuffer.EndsWith("\n") && !_readBuffer.EndsWith("\r")) ;

				var result = _readBuffer;
				_isReading = false;
				_readBuffer = string.Empty;
				return result;
			});
		}

		public void Write(char ch)
		{
			_buffer[_cursorRow, _cursorColumn].ForegroundColor = new SolidColorBrush(ForegroundColor);
			_buffer[_cursorRow, _cursorColumn].BackgroundColor = new SolidColorBrush(BackgroundColor);
			_buffer[_cursorRow, _cursorColumn].Character = ch;
			_redrawList.Add(new ConsolePosition(_cursorRow, _cursorColumn));

			_cursorColumn++;
			if (_cursorColumn >= Columns)
			{
				_cursorColumn = 0;

				// TODO: Implement auto-scrolling.
				_cursorRow++;
				if (_cursorRow >= Rows)
				{
					_cursorRow = 0;
				}
			}

			Dispatcher.Invoke(() => InvalidateVisual());
		}

		public void Write(string text, params object[] args)
		{
			text = string.Format(text, args);
			foreach (var ch in text)
			{
				Write(ch);
			}
		}

		public void WriteLine()
		{
			_cursorColumn = 0;
			_cursorRow++;

			if (_cursorRow >= Rows)
			{
				_cursorRow--;

				if (ScrollAtBottom)
				{
					// Move all of the text up 1 line.
					for (var row = 1; row < Rows; row++)
					{
						for (var column = 0; column < Columns; column++)
						{
							_buffer[row - 1, column] = _buffer[row, column];
							_redrawList.Add(new ConsolePosition(row - 1, column));
						}
					}

					// Clear out the bottom row.
					for (var column = 0; column < Columns; column++)
					{
						_buffer[Rows - 1, column].BackgroundColor = new SolidColorBrush(BackgroundColor);
						_buffer[Rows - 1, column].ForegroundColor = new SolidColorBrush(ForegroundColor);
						_buffer[Rows - 1, column].Character = '\0';
						_redrawList.Add(new ConsolePosition(Rows - 1, column));
					}
				}
				else
				{
					_cursorRow = 0;
				}
			}
		}

		public void WriteLine(string text, params object[] args)
		{
			Write(text, args);
			WriteLine();
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			var visual = new DrawingVisual();
			using (var drawingContext = visual.RenderOpen())
			{
				// Merge the last frame's back buffer with the latest updates.
				drawingContext.DrawImage(_backBuffer, new Rect(0, 0, Width, Height));
				RenderUpdates(drawingContext);
			}
			// Save the back buffer for the next frame.
			_backBuffer.Render(visual);

			// Render the back buffer to the canvas.
			dc.DrawImage(_backBuffer, new Rect(0, 0, Width, Height));
		}

		/// <summary>
		/// Render any new changes to the drawing context.
		/// </summary>
		private void RenderUpdates(DrawingContext dc)
		{
			foreach (var point in _redrawList)
			{
				var location = new Point(point.Column * _asciiTiles.TileWidth, point.Row * _asciiTiles.TileHeight);
				var dstRect = new Rect(location, _tileSize);

				var attribute = _buffer[point.Row, point.Column];
				var backgroundBrush = attribute.BackgroundColor;
				dstRect.Location = location;

				dc.DrawRectangle(backgroundBrush, null, dstRect);

				_asciiTiles.Render(dc, location, attribute.ForegroundColor, attribute.Character);
			}
			_redrawList.Clear();
		}

		/// <summary>
		/// Render the entire console buffer to the drawing context.
		/// </summary>
		/// <remarks>
		/// This should never be necessary, as it's much more efficient to render the redraw list.
		/// </remarks>
		private void RenderBuffer(DrawingContext dc)
		{
			var location = new Point(0, 0);
			var dstRect = new Rect(location, _tileSize);
			for (int row = 0, y = 0; row < Rows; row++, y += _asciiTiles.TileHeight)
			{
				location.X = 0;
				for (int column = 0, x = 0; column < Columns; column++, x += _asciiTiles.TileWidth)
				{
					var attribute = _buffer[row, column];
					var backgroundBrush = _buffer[row, column].BackgroundColor;
					dstRect.Location = location;

					dc.DrawRectangle(backgroundBrush, null, dstRect);

					_asciiTiles.Render(dc, location, attribute.ForegroundColor, attribute.Character);

					location.X += _asciiTiles.TileWidth;
				}
				location.Y += _asciiTiles.TileHeight;
			}
		}

		#endregion

		#region Event Handlers

		private void Keyboard_KeyUp(object sender, RawKeyEventArgs args)
		{
		}

		private void Keyboard_KeyDown(object sender, RawKeyEventArgs args)
		{
			if (_isReading)
			{
				_readBuffer += args.Character;
				if (args.Key == Key.Enter)
				{
					WriteLine();
				}
				else
				{
					Write(args.Character);
				}
			}
		}

		#endregion
	}
}
