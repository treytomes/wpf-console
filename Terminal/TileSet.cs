using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Terminal
{
	public class TileSet
	{
		#region Constants

		private const int BITS_PER_BYTE = 8;

		#endregion

		#region Fields

		public WriteableBitmap _source;
		private List<CroppedBitmap> _tiles;
		private int _tilesPerRow;
		private int _bytesPerPixel;
		private int _stride;
		private byte[] _originalPixels;

		#endregion

		#region Constructors

		public TileSet(System.Drawing.Bitmap source, int tileWidth, int tileHeight)
		{
			_source = new WriteableBitmap(Imaging.CreateBitmapSourceFromHBitmap(
				source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
				BitmapSizeOptions.FromWidthAndHeight(source.Width, source.Height)));

			TileWidth = tileWidth;
			TileHeight = tileHeight;
			_tilesPerRow = (int)_source.Width / TileWidth;

			_bytesPerPixel = _source.Format.BitsPerPixel / BITS_PER_BYTE;
			_stride = _source.PixelWidth * _bytesPerPixel;
			_originalPixels = new byte[_stride * _source.PixelHeight];
			_source.CopyPixels(_originalPixels, _stride, 0);

			_tiles = new List<CroppedBitmap>();
			GenerateTiles();
		}

		#endregion

		#region Properties

		public int TileWidth { get; private set; }

		public int TileHeight { get; private set; }

		#endregion

		#region Methods

		public void Render(DrawingContext dc, Point location, Color tint, int tileIndex)
		{
			Render(dc, location, new SolidColorBrush(tint), tileIndex);
		}

		public void Render(DrawingContext dc, Point location, Brush tint, int tileIndex)
		{
			var dstRect = new Rect(location, new Size(TileWidth, TileHeight));

			dc.PushOpacityMask(new ImageBrush(_tiles[tileIndex]));
			dc.DrawRectangle(tint, null, dstRect);

			dc.Pop();
		}

		public void RenderString(DrawingContext dc, Point location, Color tint, string text, params object[] args)
		{
			text = string.Format(text, args);
			for (var index = 0; index < text.Length; index++, location.X += TileWidth)
			{
				Render(dc, location, tint, text[index]);
			}
		}

		private Int32Rect GetTileBounds(int tileIndex)
		{
			return new Int32Rect(
				(tileIndex % _tilesPerRow) * TileWidth,
				(tileIndex / _tilesPerRow) * TileHeight,
				TileWidth,
				TileHeight);
		}

		private void GenerateTiles()
		{
			_tiles.Clear();
			var numRows = _source.Height / TileHeight;
			for (var tileIndex = 0; tileIndex < numRows * _tilesPerRow; tileIndex++)
			{
				var tileBounds = GetTileBounds(tileIndex);
				_tiles.Add(new CroppedBitmap(_source, tileBounds));
			}
		}

		#endregion
	}
}
