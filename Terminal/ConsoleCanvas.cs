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
			public readonly Color ForegroundColor;
			public readonly Color BackgroundColor;
			public readonly char Character;

			public readonly SolidColorBrush ForegroundBrush;
			public readonly SolidColorBrush BackgroundBrush;

			public ConsoleAttribute(char character, Color foregroundColor, Color backgroundColor)
			{
				Character = character;
				ForegroundColor = foregroundColor;
				BackgroundColor = backgroundColor;
				ForegroundBrush = new SolidColorBrush(ForegroundColor);
				BackgroundBrush = new SolidColorBrush(BackgroundColor);
			}

			public override bool Equals(object obj)
			{
				if ((obj == null) || !(obj is ConsoleAttribute))
				{
					return false;
				}
				else
				{
					var other = (ConsoleAttribute)obj;
					return
						(ForegroundColor == other.ForegroundColor) &&
						(BackgroundColor == other.BackgroundColor) &&
						(Character == other.Character);
				}
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					// Check for null values here...
					var hash = 17;
					hash = hash * 23 + ForegroundColor.GetHashCode();
					hash = hash * 23 + BackgroundColor.GetHashCode();
					hash = hash * 23 + Character.GetHashCode();
					return hash;
				}
			}

			public static bool operator == (ConsoleAttribute left, ConsoleAttribute right)
			{
				return left.Equals(right);
			}

			public static bool operator != (ConsoleAttribute left, ConsoleAttribute right)
			{
				return !left.Equals(right); // !(left == right);
			}
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

		private ConsoleAttribute this[int row, int column]
		{
			get
			{
				return _buffer[row, column];
			}
			set
			{
				if (_buffer[row, column] != value)
				{
					_buffer[row, column] = value;
					_redrawList.Add(new ConsolePosition(row, column));
				}
			}
		}

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
			this[_cursorRow, _cursorColumn] = new ConsoleAttribute(ch, ForegroundColor, BackgroundColor);

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
							this[row - 1, column] = this[row, column];
						}
					}

					var emptyAttr = new ConsoleAttribute('\0', ForegroundColor, BackgroundColor);

					// Clear out the bottom row.
					for (var column = 0; column < Columns; column++)
					{
						this[Rows - 1, column] = emptyAttr;
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

				var attribute = this[point.Row, point.Column];
				var backgroundBrush = attribute.BackgroundBrush;
				dstRect.Location = location;

				dc.DrawRectangle(backgroundBrush, null, dstRect);

				_asciiTiles.Render(dc, location, attribute.ForegroundBrush, attribute.Character);
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
					var attribute = this[row, column];
					var backgroundBrush = this[row, column].BackgroundBrush;
					dstRect.Location = location;

					dc.DrawRectangle(backgroundBrush, null, dstRect);

					_asciiTiles.Render(dc, location, attribute.ForegroundBrush, attribute.Character);

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
