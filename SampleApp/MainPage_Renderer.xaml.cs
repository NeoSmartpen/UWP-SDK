using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Neosmartpen.Net;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace SampleApp
{
    public sealed partial class MainPage : Page
    {
        private CanvasRenderTarget _canvasCurrent, _canvasArchived;

        private CanvasStrokeStyle _canvasStrokeStyle;

        private Stroke _stroke;

        private float _scale;

        public const float Pixel2DotScaleFactor = 600f / 72f / 56f;

        public static Color _color;

        public PaperInformation _currentPaperInfo;

        private void initStrokesStyle()
        {
            _canvasStrokeStyle = new CanvasStrokeStyle();
            _canvasStrokeStyle.TransformBehavior = CanvasStrokeTransformBehavior.Fixed;
            _canvasStrokeStyle.StartCap = CanvasCapStyle.Round;
            _canvasStrokeStyle.EndCap = CanvasCapStyle.Round;
            _canvasStrokeStyle.DashStyle = CanvasDashStyle.Solid;
            _canvasStrokeStyle.DashCap = CanvasCapStyle.Round;
            _canvasStrokeStyle.LineJoin = CanvasLineJoin.Round;
            
            _color = Colors.Black;
        }

        private void drawableCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_canvasCurrent == null || _canvasArchived == null)
            {
                return;
            }

            float originPointX = (float)(sender.Size.Width - _canvasCurrent.Size.Width) / 2f;
            float originPointY = (float)(sender.Size.Height - _canvasCurrent.Size.Height) / 2f;

            args.DrawingSession.DrawImage(_canvasArchived, originPointX, originPointY);
            args.DrawingSession.DrawImage(_canvasCurrent, originPointX, originPointY);
            args.DrawingSession.Flush();
        }

        private void drawableCanvas_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            drawableCanvas_CreateResources(sender as CanvasControl, null);
        }

        private void drawableCanvas_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            if (_currentPaperInfo == null)
            {
                return;
            }

            _scale = Math.Min(
                ((float)sender.Size.Width) / (_currentPaperInfo.Width * Pixel2DotScaleFactor),
                ((float)sender.Size.Height) / (_currentPaperInfo.Height * Pixel2DotScaleFactor)
            );

            float docWidth = _currentPaperInfo.Width * Pixel2DotScaleFactor * _scale;
            float docHeight = _currentPaperInfo.Height * Pixel2DotScaleFactor * _scale;

            CanvasDevice device = CanvasDevice.GetSharedDevice();

            _canvasCurrent = new CanvasRenderTarget(device, docWidth, docHeight, sender.Dpi);
            _canvasArchived = new CanvasRenderTarget(device, docWidth, docHeight, sender.Dpi);

            ClearCanvas(_canvasCurrent, Colors.Transparent);
            ClearCanvas(_canvasArchived);

            drawableCanvas.Invalidate();
        }
        private async void MController_OfflineStrokeReceived(IPenClient sender, OfflineStrokeReceivedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {

                foreach (Stroke stroke in args.Strokes)
                {
                    DrawStroke(_canvasArchived, stroke);
                }

                _progressDialog.Update(args.AmountDone, args.Total);
            });
        }

        private void ProcessDot(Dot dot)
        {
            if (_stroke == null)
            {
                _stroke = new Stroke(dot.Section, dot.Owner, dot.Note, dot.Page);
            }

            _stroke.Add(dot);

            // 임시 획을 그린다.
            DrawStroke(_canvasCurrent, _stroke.Count > 1 ? _stroke.GetRange(_stroke.Count - 2, 2): _stroke.GetRange(_stroke.Count - 1, 1));

            if (dot.DotType == DotTypes.PEN_UP)
            {
                // 최종 획을 그린다.
                DrawStroke(_canvasArchived, _stroke);

                // 모든 임시 획들을 지운다.
                ClearCanvas(_canvasCurrent, Colors.Transparent);

                _stroke.Clear();
                _stroke = null;
            }
        }

        private void DrawStroke(CanvasRenderTarget target, List<Dot> dots)
        {
            float offsetX = _currentPaperInfo.OffsetX * _scale * Pixel2DotScaleFactor;
            float offsetY = _currentPaperInfo.OffsetY * _scale * Pixel2DotScaleFactor;

            DrawToCanvas(target, dots, _scale, -offsetX, -offsetY, _color, _canvasStrokeStyle);

            drawableCanvas.Invalidate();
        }

        private void DrawToCanvas(CanvasRenderTarget crt, List<Dot> dots, float scale, float offsetX, float offsetY, Color color, CanvasStrokeStyle canvasStrokeStyle, float thickness = 1f)
        {
            if (crt == null)
            {
                return;
            }

            if (dots == null || dots.Count == 0)
            {
                return;
            }

            using (CanvasDrawingSession drawSession = crt.CreateDrawingSession())
            {
                if (dots.Count <= 2)
                {
                    float p = (float)dots[dots.Count-1].Force / 1024 * thickness;

                    if (dots.Count == 1) // 점찍기
                    {
                        drawSession.FillCircle(dots[0].X * scale + offsetX, dots[0].Y * scale + offsetY, p, color);
                    }
                    else if (dots.Count == 2) // 선그리기
                    {
                        drawSession.DrawLine(dots[0].X * scale + offsetX, dots[0].Y * scale + offsetY, dots[1].X * scale + offsetX, dots[1].Y * scale + offsetY, color, p, canvasStrokeStyle);
                    }
                }
                else
                {
                    thickness /= 2;

                    float x0, x1, x2, x3, y0, y1, y2, y3, p0, p1, p2, p3;
                    float vx01, vy01, vx21, vy21;
                    float norm;
                    float n_x0, n_y0, n_x2, n_y2;

                    x0 = dots[0].X * scale + offsetX + 0.1f;
                    y0 = dots[0].Y * scale + offsetY;
                    // TODO Change MaxForce
                    p0 = (float)dots[0].Force / 1024 * thickness;

                    x1 = dots[1].X * scale + offsetX + 0.1f;
                    y1 = dots[1].Y * scale + offsetY;
                    p1 = (float)dots[1].Force / 1024 * thickness;

                    vx01 = x1 - x0;
                    vy01 = y1 - y0;
                    // instead of dividing tangent/norm by two, we multiply norm by 2
                    norm = (float)System.Math.Sqrt(vx01 * vx01 + vy01 * vy01 + 0.0001f) * 2f;
                    //vx01 = vx01 / norm * scaled_pen_thickness * p0;
                    //vy01 = vy01 / norm * scaled_pen_thickness * p0;
                    vx01 = vx01 / norm * p0;
                    vy01 = vy01 / norm * p0;
                    n_x0 = vy01;
                    n_y0 = -vx01;

                    CanvasPathBuilder pathBuilder;

                    int count = dots.Count;

                    for (int i = 2; i < count; ++i)
                    {
                        x3 = dots[i].X * scale + offsetX + 0.1f;
                        y3 = dots[i].Y * scale + offsetY;
                        p3 = (float)dots[i].Force / 1024 * thickness;

                        x2 = (x1 + x3) / 2.0f;
                        y2 = (y1 + y3) / 2.0f;
                        p2 = (p1 + p3) / 2.0f;
                        vx21 = x1 - x2;
                        vy21 = y1 - y2;
                        norm = (float)System.Math.Sqrt(vx21 * vx21 + vy21 * vy21 + 0.0001f) * 2.0f;
                        vx21 = vx21 / norm * p2;
                        vy21 = vy21 / norm * p2;
                        n_x2 = -vy21;
                        n_y2 = vx21;

                        pathBuilder = new CanvasPathBuilder(drawableCanvas);
                        pathBuilder.BeginFigure(x0 + n_x0, y0 + n_y0);
                        // The + boundary of the stroke
                        pathBuilder.AddCubicBezier(new Vector2(x1 + n_x0, y1 + n_y0), new Vector2(x1 + n_x2, y1 + n_y2), new Vector2(x2 + n_x2, y2 + n_y2));
                        // round out the cap
                        pathBuilder.AddCubicBezier(new Vector2(x2 + n_x2 - vx21, y2 + n_y2 - vy21), new Vector2(x2 - n_x2 - vx21, y2 - n_y2 - vy21), new Vector2(x2 - n_x2, y2 - n_y2));
                        // THe - boundary of the stroke
                        pathBuilder.AddCubicBezier(new Vector2(x1 - n_x2, y1 - n_y2), new Vector2(x1 - n_x0, y1 - n_y0), new Vector2(x0 - n_x0, y0 - n_y0));
                        // round out the other cap
                        pathBuilder.AddCubicBezier(new Vector2(x0 - n_x0 - vx01, y0 - n_y0 - vy01), new Vector2(x0 + n_x0 - vx01, y0 + n_y0 - vy01), new Vector2(x0 + n_x0, y0 + n_y0));
                        pathBuilder.EndFigure(CanvasFigureLoop.Open);
                        drawSession.DrawGeometry(CanvasGeometry.CreatePath(pathBuilder), color, p2);

                        x0 = x2;
                        y0 = y2;
                        p0 = p2;
                        x1 = x3;
                        y1 = y3;
                        p1 = p3;
                        vx01 = -vx21;
                        vy01 = -vy21;
                        n_x0 = n_x2;
                        n_y0 = n_y2;
                    }

                    x2 = dots[count - 1].X * scale + offsetX + 0.1f;
                    y2 = dots[count - 1].Y * scale + offsetY;
                    p2 = dots[count - 1].Force / 1024 * thickness;

                    vx21 = x1 - x2;
                    vy21 = y1 - y2;
                    norm = (float)System.Math.Sqrt(vx21 * vx21 + vy21 * vy21 + 0.0001f) * 2f;
                    //vx21 = vx21 / norm * scaled_pen_thickness * p2;
                    //vy21 = vy21 / norm * scaled_pen_thickness * p2;
                    vx21 = vx21 / norm * p2;
                    vy21 = vy21 / norm * p2;
                    n_x2 = -vy21;
                    n_y2 = vx21;

                    pathBuilder = new CanvasPathBuilder(drawableCanvas);
                    pathBuilder.BeginFigure(x0 + n_x0, y0 + n_y0);
                    pathBuilder.AddCubicBezier(new Vector2(x1 + n_x0, y1 + n_y0), new Vector2(x1 + n_x2, y1 + n_y2), new Vector2(x2 + n_x2, y2 + n_y2));
                    pathBuilder.AddCubicBezier(new Vector2(x2 + n_x2 - vx21, y2 + n_y2 - vy21), new Vector2(x2 - n_x2 - vx21, y2 - n_y2 - vy21), new Vector2(x2 - n_x2, y2 - n_y2));
                    pathBuilder.AddCubicBezier(new Vector2(x1 - n_x2, y1 - n_y2), new Vector2(x1 - n_x0, y1 - n_y0), new Vector2(x0 - n_x0, y0 - n_y0));
                    pathBuilder.AddCubicBezier(new Vector2(x0 - n_x0 - vx01, y0 - n_y0 - vy01), new Vector2(x0 + n_x0 - vx01, y0 + n_y0 - vy01), new Vector2(x0 + n_x0, y0 + n_y0));
                    pathBuilder.EndFigure(CanvasFigureLoop.Open);
                    drawSession.DrawGeometry(CanvasGeometry.CreatePath(pathBuilder), color, p2);
                }
            }
        }

        private void ClearCanvas(CanvasRenderTarget crt, Color? color = null)
        {
            using (CanvasDrawingSession canvasDrawSession = crt.CreateDrawingSession())
            {
                canvasDrawSession.Clear(color ?? Colors.White);
            }
        }

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

            initStrokesStyle();
        }

        private void cbPaperInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PaperInformation info = e.AddedItems[0] as PaperInformation;

            _currentPaperInfo = info;

            drawableCanvas_CreateResources(drawableCanvas, null);
        }

        private void btnClear_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ClearCanvas(_canvasCurrent, Colors.Transparent);
            ClearCanvas(_canvasArchived);

            drawableCanvas.Invalidate();
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