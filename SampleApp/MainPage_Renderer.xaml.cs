using Microsoft.Graphics.Canvas.UI.Xaml;
using Neosmartpen.Net;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace SampleApp
{
    public sealed partial class MainPage : Page
    {
		private static readonly int[] THICKNESS_LEVEL = { 1, 2, 5, 9, 18 };

        private CachedDrawableStroke _currentStroke;

        private List<CachedDrawableStroke> _strokes;

        private Dot _cursorDot;

        private float _scale = 0f;

        public const float Pixel2DotScaleFactor = 600f / 72f / 56f;

        public static Color _color;
		public int _thickness;

        public PaperInformation _currentPaperInfo;

        private object _renderLock = new object();

        public void InitRenderer()
        {
            PaperInformation paper1 = new PaperInformation("Idea Pad", 609, 595.275f, 771.023f, 36.8503f, 107.716f);
            PaperInformation paper2 = new PaperInformation("RingNote", 603, 425.196f, 595.275f, 36.8503f, 36.8503f);
            PaperInformation paper3 = new PaperInformation("Idea Pad Mini", 620, 360f, 566.929f, 36.8503f, 36.8503f);
            PaperInformation paper4 = new PaperInformation("A4", 0, 595f, 842f, 0f, 0f);
            PaperInformation paper5 = new PaperInformation("College Note", 617, 612.283f, 793.7f, 36.8503f, 36.8503f);
            PaperInformation paper6 = new PaperInformation("Plain Note", 604, 498.897f, 708.661f, 36.8503f, 36.8503f);

            cbPaperInfo.Items.Add(paper1);
            cbPaperInfo.Items.Add(paper2);
            cbPaperInfo.Items.Add(paper3);
            cbPaperInfo.Items.Add(paper4);
            cbPaperInfo.Items.Add(paper5);
            cbPaperInfo.Items.Add(paper6);

            _currentPaperInfo = paper1;

            cbPaperInfo.SelectedIndex = 0;

            _strokes = new List<CachedDrawableStroke>();
        }

		private void ChangeThinknessLevel(int index)
		{
			if (index < 0) index = 0;
			else if (index > 4) index = 4;
			_thickness = THICKNESS_LEVEL[index];
		}

		private void drawableCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            lock (_renderLock)
            {
                args.DrawingSession.Clear(Colors.LightGray);

                if (_scale <= 0f)
                    return;

                float docWidth = _currentPaperInfo.Width * Pixel2DotScaleFactor * _scale;
                float docHeight = _currentPaperInfo.Height * Pixel2DotScaleFactor * _scale;

                float originPointX = (float)(sender.Size.Width - docWidth) / 2f;
                float originPointY = (float)(sender.Size.Height - docHeight) / 2f;

                args.DrawingSession.FillRectangle(originPointX, originPointY, docWidth, docHeight, Colors.White);

                float offsetX = _currentPaperInfo.OffsetX * _scale * Pixel2DotScaleFactor;
                float offsetY = _currentPaperInfo.OffsetY * _scale * Pixel2DotScaleFactor;

                if (_currentStroke != null)
                {
                    _currentStroke.Draw(args.DrawingSession, _scale, -offsetX + originPointX, -offsetY + originPointY);
                }

                if (_strokes != null && _strokes.Count > 0)
                {
                    foreach (var st in _strokes)
                    {
                        st.Draw(args.DrawingSession, _scale, -offsetX + originPointX, -offsetY + originPointY);
                    }
                }

                if (_cursorDot != null)
                {
                    float x = (_cursorDot.X * _scale) - (offsetX) + originPointX;
                    float y = (_cursorDot.Y * _scale) - (offsetY) + originPointY;
                    args.DrawingSession.DrawCircle(x, y, 3, Colors.Red, 1);
                }

                args.DrawingSession.Flush();
            }
        }

        private void drawableCanvas_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            drawableCanvas_CreateResources(sender as CanvasControl, null);
        }

        private void drawableCanvas_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            lock (_renderLock)
            {
                if (_currentPaperInfo == null)
                {
                    return;
                }

                _scale = Math.Min(
                    ((float)sender.Size.Width) / (_currentPaperInfo.Width * Pixel2DotScaleFactor),
                    ((float)sender.Size.Height) / (_currentPaperInfo.Height * Pixel2DotScaleFactor)
                );

                drawableCanvas.Invalidate();
            }
        }

        private async void MController_OfflineStrokeReceived(IPenClient sender, OfflineStrokeReceivedEventArgs args)
        {
            foreach (Stroke stroke in args.Strokes)
                _strokes.Add(new CachedDrawableStroke(_color, _thickness, stroke));

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                _progressDialog.Update(args.AmountDone, args.Total);
            });

            drawableCanvas.Invalidate();
        }

        private int CurrNote = -1, CurrPage = -1;

        private void ProcessDot(Dot dot)
        {
            lock (_renderLock)
            {
                if (dot.Note != CurrNote || dot.Page != CurrPage)
                {
                    _strokes.Clear();

                    CurrNote = dot.Note;
                    CurrPage = dot.Page;
                }

                if (dot.DotType == DotTypes.PEN_HOVER)
                {
                    _cursorDot = dot;
                }
                else
                {
                    if (_currentStroke == null)
                    {
                        _currentStroke = new CachedDrawableStroke(_color, _thickness);
                    }

                    _currentStroke.Add(dot);
                }

                if (dot.DotType == DotTypes.PEN_UP)
                {
                    _strokes.Add(_currentStroke);
                    _currentStroke = null;
                }

                drawableCanvas.Invalidate();
            }
        }

        private void cbPaperInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock (_renderLock)
            {
                PaperInformation info = e.AddedItems[0] as PaperInformation;
                _currentPaperInfo = info;
                drawableCanvas_CreateResources(drawableCanvas, null);
            }
        }

        private void btnClear_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            lock (_renderLock)
            {
                foreach (var st in _strokes)
                {
                    st.Dispose();
                }
                _strokes.Clear();
                drawableCanvas.Invalidate();
            }
        }

        public class PaperInformation
        {
            public string Title;
            public float Width, Height, OffsetX, OffsetY;
            public int BookId;

            public PaperInformation(string title, int bookId, float width, float height, float offsetx, float offsety)
            {
                Title = title;
                BookId = bookId;
                Width = width;
                Height = height;
                OffsetX = offsetx;
                OffsetY = offsety;
            }

            public override string ToString()
            {
                return Title;
            }
        }
    }
}