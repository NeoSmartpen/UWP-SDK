using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Neosmartpen.Net;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace SampleApp
{
    /// <summary>
    /// Class that provides the function to draw the coordinate data received from the pen on the screen.
    /// (Caching for path is supported, so you can draw quickly when you redraw after initial drawing.)
    /// </summary>
    public class CachedDrawableStroke : IDisposable
    {
        public enum InterpolationType { Catmullrom, Geometry }

        private CanvasCachedGeometry CanvasCachedGeometry;

        private List<DrawablePoint> points, midPoints, leftPoints, rightPoints, filtered;

        private float scale, thickness;

        /// <summary>
        /// Gets or sets scale of stroke.
        /// </summary>
        public float Scale { get { return scale; } set { if (scale != value) { CanvasCachedGeometry?.Dispose(); CanvasCachedGeometry = null; scale = value; } } }

        /// <summary>
        /// Gets or sets color of stroke.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets thickness of stroke.
        /// </summary>
        public float Thickness { get { return thickness; } set { if (thickness != value) { CanvasCachedGeometry?.Dispose(); CanvasCachedGeometry = null; thickness = value; } } }

        private bool ReadyToDraw { get { return CanvasCachedGeometry != null; } }

        public CachedDrawableStroke(List<Dot> dots = null)
        {
            points = new List<DrawablePoint>();
            midPoints = new List<DrawablePoint>();
            leftPoints = new List<DrawablePoint>();
            rightPoints = new List<DrawablePoint>();
            filtered = new List<DrawablePoint>();

            Scale = 1f;
            Color = Colors.Black;
            Thickness = 1f;

            if (dots != null)
                foreach (Dot dot in dots)
                    points.Add(new DrawablePoint(dot));

        }

        public CachedDrawableStroke(Color color, float thickness = 1f, List<Dot> dots = null) : this(dots)
        {
            Color = color;
            Thickness = thickness;
        }

        public CachedDrawableStroke(float thickness = 1f, List<Dot> dots = null) : this(dots)
        {
            Color = Colors.Black;
            Thickness = thickness;
        }

        /// <summary>
        /// Adds a dot to the current stroke.
        /// </summary>
        /// <param name="dot">Coordinate data received from the pen</param>
        public void Add(Dot dot)
        {
            CanvasCachedGeometry?.Dispose();
            CanvasCachedGeometry = null;
            points.Add(new DrawablePoint(dot));
        }

        /// <summary>
        /// Draw the current stroke on the CanvasDrawingSession.
        /// </summary>
        /// <param name="canvasDrawingSession">CanvasDrawingSession to be drawn</param>
        /// <param name="x">offset x coordinate</param>
        /// <param name="y">offset y coordinate</param>
        public void Draw(CanvasDrawingSession canvasDrawingSession, float x = 0f, float y = 0f)
        {
            Build();
            canvasDrawingSession.DrawCachedGeometry(CanvasCachedGeometry, x, y, Color);
            //foreach (var fp in points)
            //    canvasDrawingSession.FillCircle(new Vector2(fp.X / 56 * Scale + x, fp.Y / 56 * Scale + y), 2, Colors.LightYellow);
            //foreach (var fp in filtered)
            //    canvasDrawingSession.FillCircle(new Vector2(fp.X / 56 * Scale + x, fp.Y / 56 * Scale + y), 2, Colors.LightPink);
        }

        /// <summary>
        /// Draw the current stroke on the CanvasDrawingSession.
        /// </summary>
        /// <param name="canvasDrawingSession">CanvasDrawingSession to be drawn</param>
        /// <param name="scale">set new scale</param>
        /// <param name="x">offset x coordinate</param>
        /// <param name="y">offset y coordinate</param>
        public void Draw(CanvasDrawingSession canvasDrawingSession, float scale, float x = 0f, float y = 0f)
        {
            Scale = scale;
            Draw(canvasDrawingSession, x, y);
        }

        /// <summary>
        /// Draw the current stroke on the CanvasDrawingSession.
        /// </summary>
        /// <param name="canvasDrawingSession">CanvasDrawingSession to be drawn</param>
        /// <param name="scale">set new scale</param>
        /// <param name="color">set new color</param>
        /// <param name="thickness">set new thickness</param>
        /// <param name="x">offset x coordinate</param>
        /// <param name="y">offset y coordinate</param>
        public void Draw(CanvasDrawingSession canvasDrawingSession, float scale, Color color, float thickness, float x = 0f, float y = 0f)
        {
            Scale = scale;
            Color = color;
            Thickness = thickness;
            Draw(canvasDrawingSession, x, y);
        }

        private void Build()
        {
            if (ReadyToDraw)
                return;

            filtered.Clear();
            CanvasCachedGeometry = CreateCachedGeometry(Scale, Thickness);

            //Random rand = new Random();
            //if (rand.NextDouble() < 0.5f)
            //{
            //    Color = Colors.Red;
            //    CanvasCachedGeometry = CreateCachedGeometry(Scale, Thickness);
            //}
            //else
            //{
            //    Color = Colors.Blue;
            //    CanvasCachedGeometry = CreateCachedGeometryPreviousVersion(Scale, Thickness);
            //}

            midPoints.Clear();
            leftPoints.Clear();
            rightPoints.Clear();
        }

        private const float Pixel2DotScaleFactor = 600f / 72f / 56f;
        private const float PenInkFactor = 0.68f / 25.4f * 72f;

        public static float CalcForce(float force, float scale, float thickness)
        {
            float cforce = PenInkFactor * Pixel2DotScaleFactor * (force / 1024f) * scale * thickness / 2.3f;
            return cforce;
        }

        private static CanvasStrokeStyle StrokeStyle;
        public static CanvasStrokeStyle GetStrokesStyle()
        {
            if (StrokeStyle == null)
            {
                StrokeStyle = new CanvasStrokeStyle
                {
                    TransformBehavior = CanvasStrokeTransformBehavior.Fixed,
                    StartCap = CanvasCapStyle.Round,
                    EndCap = CanvasCapStyle.Round,
                    DashStyle = CanvasDashStyle.Solid,
                    DashCap = CanvasCapStyle.Round,
                    LineJoin = CanvasLineJoin.Round
                };
            }
            return StrokeStyle;
        }

        private CanvasCachedGeometry CreateCachedGeometryPreviousVersion(float scale, float thickness)
        {
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            if (device == null)
                return null;

            foreach(var pt in points)
            {
                midPoints.Add(new DrawablePoint(pt.X / 56f, pt.Y / 56f, pt.Force * pt.MaxForce));
            }

            var dots = midPoints;

            if (dots.Count <= 2)
            {
                float p = CalcForce(dots[0].Force, scale, thickness);

                if (dots.Count == 1) // 점찍기
                {
                    using (var geom = CanvasGeometry.CreateCircle(device, dots[0].X * scale, dots[0].Y * scale, p / 2))
                    {
                        return CanvasCachedGeometry.CreateFill(geom);
                    }
                }
                else if (dots.Count == 2) // 선그리기
                {
                    float p2 = CalcForce(dots[0].Force, scale, thickness);

                    CanvasPathBuilder pathBuilder = new CanvasPathBuilder(device);
                    pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
                    pathBuilder.BeginFigure(dots[0].X * scale, dots[0].Y * scale);
                    pathBuilder.AddLine(dots[1].X * scale, dots[1].Y * scale);
                    pathBuilder.EndFigure(CanvasFigureLoop.Open);
                    using (var geom = CanvasGeometry.CreatePath(pathBuilder))
                    {
                        return CanvasCachedGeometry.CreateStroke(geom, (p + p2) / 2, GetStrokesStyle());
                    }
                }
            }
            else
            {
                float x0, x1, x2, x3, y0, y1, y2, y3, p0, p1, p2, p3;
                float vx01, vy01, vx21, vy21;
                float norm;
                float n_x0, n_y0, n_x2, n_y2;

                x0 = dots[0].X * scale + 0.1f;
                y0 = dots[0].Y * scale;

                // TODO Change MaxForce
                //p0 = (float)dots[0].Force / 1024 * width;
                p0 = CalcForce(dots[0].Force, scale, thickness) / 2;

                x1 = dots[1].X * scale + 0.1f;
                y1 = dots[1].Y * scale;

                //p1 = (float)dots[1].Force / 1024 * width;
                p1 = CalcForce(dots[1].Force, scale, thickness) / 2;

                vx01 = x1 - x0;
                vy01 = y1 - y0;

                // instead of dividing tangent/norm by two, we multiply norm by 2
                norm = (float)Math.Sqrt(vx01 * vx01 + vy01 * vy01 + 0.0001f) * 2f;
                //vx01 = vx01 / norm * scaled_pen_thickness * p0;
                //vy01 = vy01 / norm * scaled_pen_thickness * p0;
                vx01 = vx01 / norm * p0;
                vy01 = vy01 / norm * p0;
                n_x0 = vy01;
                n_y0 = -vx01;

                CanvasPathBuilder pathBuilder = new CanvasPathBuilder(device);
                pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);

                int count = dots.Count;

                for (int i = 2; i < count; ++i)
                {
                    x3 = dots[i].X * scale + 0.1f;
                    y3 = dots[i].Y * scale;

                    //p3 = (float)dots[i].Force / 1024 * width;
                    //p3 = CalcForce(dots[i].Force, scale, thickness) / 2;
                    p3 = dots[i].Force;

                    x2 = (x1 + x3) / 2.0f;
                    y2 = (y1 + y3) / 2.0f;
                    p2 = (p1 + p3) / 2.0f;

                    vx21 = x1 - x2;
                    vy21 = y1 - y2;

                    norm = (float)System.Math.Sqrt(vx21 * vx21 + vy21 * vy21 + 0.0001f) * 2.0f;

                    vx21 = vx21 / norm * CalcForce(p2, scale, thickness);
                    vy21 = vy21 / norm * CalcForce(p2, scale, thickness);

                    n_x2 = -vy21;
                    n_y2 = vx21;

                    //pathBuilder = new CanvasPathBuilder(crt.Device);
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

                x2 = dots[count - 1].X * scale + 0.1f;
                y2 = dots[count - 1].Y * scale;
                p2 = dots[count - 1].Force;

                vx21 = x1 - x2;
                vy21 = y1 - y2;

                norm = (float)Math.Sqrt(vx21 * vx21 + vy21 * vy21 + 0.0001f) * 2f;

                vx21 = vx21 / norm * CalcForce(p2, scale, thickness);
                vy21 = vy21 / norm * CalcForce(p2, scale, thickness);

                n_x2 = -vy21;
                n_y2 = vx21;

                pathBuilder.BeginFigure(x0 + n_x0, y0 + n_y0);
                pathBuilder.AddCubicBezier(new Vector2(x1 + n_x0, y1 + n_y0), new Vector2(x1 + n_x2, y1 + n_y2), new Vector2(x2 + n_x2, y2 + n_y2));
                pathBuilder.AddCubicBezier(new Vector2(x2 + n_x2 - vx21, y2 + n_y2 - vy21), new Vector2(x2 - n_x2 - vx21, y2 - n_y2 - vy21), new Vector2(x2 - n_x2, y2 - n_y2));
                pathBuilder.AddCubicBezier(new Vector2(x1 - n_x2, y1 - n_y2), new Vector2(x1 - n_x0, y1 - n_y0), new Vector2(x0 - n_x0, y0 - n_y0));
                pathBuilder.AddCubicBezier(new Vector2(x0 - n_x0 - vx01, y0 - n_y0 - vy01), new Vector2(x0 + n_x0 - vx01, y0 + n_y0 - vy01), new Vector2(x0 + n_x0, y0 + n_y0));
                pathBuilder.EndFigure(CanvasFigureLoop.Open);

                using (var geom = CanvasGeometry.CreatePath(pathBuilder))
                {
                    return CanvasCachedGeometry.CreateFill(geom);
                }
            }

            return null;
        }

        private CanvasCachedGeometry CreateCachedGeometry(float scale, float thickness, float factor = 0.5f)
        {
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            if (device == null)
                return null;

            CanvasPathBuilder pathBuilder = new CanvasPathBuilder(device);
            pathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);

            float maxThickness = thickness;
            float minThickness = thickness / 5;

            bool closedPath = false; 

            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                // 끝점 2개가 완전히 일치하면 하나만 넣는다.
                if (i > 0 && i == count - 1 && (points[i].X == points[i - 1].X && points[i].Y == points[i - 1].Y))
                    continue;
                else
                {
                    float force = points[i].Force;
                    force = (force < 0) ? 0 : points[i].Force;
                    force = (force > 1.0f) ? 1.0f : points[i].Force;

                    // 점의 두께 차이가 최대 4배가 되도록 조정
                    //float normalForce = 0.2 + ( force * 0.6 );	// 0.2 ~ 0.8
                    midPoints.Add(points[i]);
                    //midPoints.Add(new Point(new Point(points[i].X, points[i].Y, force), scale));
                }
            }

            //if (points.Count > midPoints.Count)
            //    Debug.WriteLine("filtered : " + (points.Count - midPoints.Count));

            count = midPoints.Count;

            // 선 두께에 비해 가까운 점이나 라인상 거의 필요가 없다고 판단되는 점을 지우는 부분
            if (midPoints.Count > 2)
                Simplify(midPoints, maxThickness);

            //if (count > midPoints.Count)
            //    Debug.WriteLine("simplify : " + (count - midPoints.Count));

            count = midPoints.Count;
            // 처음과 끝에 펜 구동상 꼬리가 생기는 경우 꼬리를 판단해서 제거하는 부분
            // 꼬리는 없을수도 있다.
            // 미완성
            if (midPoints.Count > 2)
                RemoveTail(midPoints, maxThickness);

            //if (count > midPoints.Count)
            //    Debug.WriteLine("removeTail : " + (count - midPoints.Count));

            count = midPoints.Count;

            // 점 제거 로직 후 점이 하나만 남으면 원으로 그린다.
            // Neo Studio에서는 점이 하나일때 가까운 점을 하나 더 추가하여 2점 처리를 한다. 
            if (count == 1)
            {
                float force = midPoints[0].Force < 0.3f ? 0.3f : midPoints[0].Force;
                //float r = maxThickness * force * 3.0f * 1.5f;
                float p = PenInkFactor * Pixel2DotScaleFactor * force * scale * maxThickness;
                using (var geom = CanvasGeometry.CreateCircle(device, midPoints[0].X, midPoints[0].Y , p / 2))
                {
                    return CanvasCachedGeometry.CreateFill(geom);
                }
            }
            else
            {
                GetControlPoint(midPoints, midPoints.Count, closedPath, factor);
                GetSplitPoints(midPoints, 0.1f, 60);
                count = midPoints.Count;

                for (int i = 0; i < count; i++)
                {
                    float px = midPoints[i].OutX - midPoints[i].InX;
                    float py = midPoints[i].OutY - midPoints[i].InY;
                    float dp = midPoints[i].GetDistanceInOut();
                    float pp = maxThickness * midPoints[i].Force;

                    pp *= 1.5f;

                    if (pp < minThickness)
                        pp = minThickness;

                    // 첫점과 끝점 처리
                    if (i == 0)
                    {
                        px = midPoints[i + 1].X - midPoints[i].X;
                        py = midPoints[i + 1].Y - midPoints[i].Y;
                        dp = (float)Math.Sqrt(px * px + py * py);
                    }
                    else if (i == midPoints.Count - 1)
                    {
                        px = midPoints[i].X - midPoints[i - 1].X;
                        py = midPoints[i].Y - midPoints[i - 1].Y;
                        dp = (float)Math.Sqrt(px * px + py * py);
                    }

                    if (dp != 0)
                    {
                        rightPoints.Add(new DrawablePoint(midPoints[i].X - (py * pp / dp), midPoints[i].Y + (px * pp / dp), midPoints[i].Force));
                        leftPoints.Add(new DrawablePoint(midPoints[i].X + (py * pp / dp), midPoints[i].Y - (px * pp / dp), midPoints[i].Force));
                    }
                    else
                    {
                        rightPoints.Add(new DrawablePoint(midPoints[i].X, midPoints[i].Y, midPoints[i].Force));
                        leftPoints.Add(new DrawablePoint(midPoints[i].X, midPoints[i].Y, midPoints[i].Force));
                    }
                }

                GetControlPoint(leftPoints, leftPoints.Count, true, factor);
                GetControlPoint(rightPoints, rightPoints.Count, true, factor);

                foreach (var pt in leftPoints)
                    pt.SetScale(scale / 56f);

                foreach (var pt in rightPoints)
                    pt.SetScale(scale / 56f);

                DrawPath(pathBuilder, leftPoints, rightPoints);

                using (var geom = CanvasGeometry.CreatePath(pathBuilder))
                {
                    return CanvasCachedGeometry.CreateFill(geom);
                }
            }
        }

        // use for shape (path)
        private CanvasPathBuilder DrawPath(CanvasPathBuilder p, List<DrawablePoint> left, List<DrawablePoint> right)
        {
            int count = left.Count;

            for (int i = 0; i < count - 1; ++i)
            {
                p.BeginFigure(left[i].X, left[i].Y);
                p.AddCubicBezier(
                    new Vector2(left[i].OutX, left[i].OutY),
                    new Vector2(left[i + 1].InX, left[i + 1].InY),
                    new Vector2(left[i + 1].X, left[i + 1].Y));

                p.AddLine(right[i + 1].X, right[i + 1].Y);
                p.AddCubicBezier(
                    new Vector2(right[i + 1].InX, right[i + 1].InY),
                    new Vector2(right[i].OutX, right[i].OutY),
                    new Vector2(right[i].X, right[i].Y));

                p.AddLine(left[i].X, left[i].Y);
                p.EndFigure(CanvasFigureLoop.Open);
            }

            float r = left[0].GetDistance(right[0]);
            AddArc(p, (left[0].X + right[0].X) / 2 - r / 2, (left[0].Y + right[0].Y) / 2 - r / 2, r, r);

            r = left[count - 1].GetDistance(right[count - 1]);
            AddArc(p, (left[count - 1].X + right[count - 1].X) / 2 - r / 2, (left[count - 1].Y + right[count - 1].Y) / 2 - r / 2, r, r);

            return p;
        }

        private void AddArc(CanvasPathBuilder p, float x, float y, float width, float height)
        {
            p.BeginFigure(x + width, y + height / 2);
            p.AddArc(new Vector2(x + width / 2, y + height / 2), width / 2, height / 2, 0, (float)(Math.PI * 2));
            p.EndFigure(CanvasFigureLoop.Open);
        }

        private void GetControlPoint(List<DrawablePoint> p, int count, bool closePath, float factor, InterpolationType type = InterpolationType.Catmullrom)
        {
            DrawablePoint p0 = new DrawablePoint();   // prev point
            DrawablePoint p1 = new DrawablePoint();   // current point
            DrawablePoint p2 = new DrawablePoint();   // next point

            for (int i = 0; i < count; i++)
            {
                p1.Set(p[i]);

                if (i == 0)
                {
                    if (closePath)
                        p0.Set(p[count - 1]);
                    else
                        p0.Set(p1);
                }
                else
                {
                    p0.Set(p[i - 1]);
                }

                if (i == count - 1)
                {
                    if (closePath)
                        p2.Set(p[0]);
                    else
                        p2.Set(p1);
                }
                else
                    p2.Set(p[i + 1]);

                float d1 = p0.GetDistance(p1);
                float d2 = p1.GetDistance(p2);

                if (type == InterpolationType.Catmullrom)
                {
                    float d1_a = (float)Math.Pow(d1, factor); // factor 기본 값 : 0.5
                    float d1_2a = d1_a * d1_a;
                    float d2_a = (float)Math.Pow(d2, factor);
                    float d2_2a = d2_a * d2_a;

                    if (i != 0 || closePath)
                    {
                        float A = 2 * d2_2a + 3 * d2_a * d1_a + d1_2a;
                        float N = 3 * d2_a * (d2_a + d1_a);

                        if (N != 0)
                            p[i].SetIn((d2_2a * p0.X + A * p1.X - d1_2a * p2.X) / N, (d2_2a * p0.Y + A * p1.Y - d1_2a * p2.Y) / N);
                        else
                            p[i].SetIn(p1.X, p1.Y);
                    }
                    else
                        p[i].SetIn(p1.X, p1.Y);

                    if (i != count - 1 || closePath)
                    {
                        float A = 2 * d1_2a + 3 * d1_a * d2_a + d2_2a;
                        float N = 3 * d1_a * (d1_a + d2_a);

                        if (N != 0)
                            p[i].SetOut((d1_2a * p2.X + A * p1.X - d2_2a * p0.X) / N, (d1_2a * p2.Y + A * p1.Y - d2_2a * p0.Y) / N);
                        else
                            p[i].SetOut(p1.X, p1.Y);
                    }
                    else
                        p[i].SetOut(p1.X, p1.Y);
                }
                else
                {
                    float vx = p0.X - p2.X;
                    float vy = p0.Y - p2.Y;
                    float t = factor;  // factor 기본값 : 0.4
                    float k = t * d1 / (d1 + d2);

                    if (i != 0 || closePath)
                        p[i].SetIn(p1.X + vx * k, p1.Y + vy * k);
                    else
                        p[i].SetIn(p1.X, p1.Y);

                    if (i != count - 1 || closePath)
                        p[i].SetOut(p1.X + vx * (k - t), p1.Y + vy * (k - t));
                    else
                        p[i].SetOut(p1.X, p1.Y);
                }
            }
        }

        // 세 점의 사이 각을 구하는 함수
        // 최적화 필요
        private float GetAngle(DrawablePoint p1, DrawablePoint p2, DrawablePoint p3)
        {
            float a, b, c;
            float angle, temp;

            a = (float)Math.Sqrt(Math.Pow(p1.X - p3.X, 2) + Math.Pow(p1.Y - p3.Y, 2));
            b = (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            c = (float)Math.Sqrt(Math.Pow(p2.X - p3.X, 2) + Math.Pow(p2.Y - p3.Y, 2));

            temp = (float)(Math.Pow(b, 2) + Math.Pow(c, 2) - Math.Pow(a, 2)) / (2 * b * c);

            angle = (float)Math.Acos(temp);
            angle *= (float)(180 / Math.PI);

            return angle;
        }

        // 이하 splitPoints를 얻기 위한 private 함수
        private DrawablePoint GetPoint12(DrawablePoint pts1, float t)
        {
            float x1 = pts1.X;
            float y1 = pts1.Y;
            float x2 = pts1.OutX;
            float y2 = pts1.OutY;

            float x12 = (x2 - x1) * t + x1;
            float y12 = (y2 - y1) * t + y1;

            return new DrawablePoint(x12, y12);
        }

        private DrawablePoint GetPoint34(DrawablePoint pts2, float t)
        {
            float x3 = pts2.InX;
            float y3 = pts2.InY;
            float x4 = pts2.X;
            float y4 = pts2.Y;

            float x34 = (x4 - x3) * t + x3;
            float y34 = (y4 - y3) * t + y3;

            return new DrawablePoint(x34, y34);
        }

        private DrawablePoint GetSplitPoint(DrawablePoint pts1, DrawablePoint pts2, float t)
        {
            float x1 = pts1.X;
            float y1 = pts1.Y;
            float x2 = pts1.OutX;
            float y2 = pts1.OutY;
            float x3 = pts2.InX;
            float y3 = pts2.InY;
            float x4 = pts2.X;
            float y4 = pts2.Y;

            float x12 = (x2 - x1) * t + x1;
            float y12 = (y2 - y1) * t + y1;

            float x23 = (x3 - x2) * t + x2;
            float y23 = (y3 - y2) * t + y2;

            float x34 = (x4 - x3) * t + x3;
            float y34 = (y4 - y3) * t + y3;

            float x123 = (x23 - x12) * t + x12;
            float y123 = (y23 - y12) * t + y12;

            float x234 = (x34 - x23) * t + x23;
            float y234 = (y34 - y23) * t + y23;

            float x1234 = (x234 - x123) * t + x123;
            float y1234 = (y234 - y123) * t + y123;

            DrawablePoint splitPoint = new DrawablePoint(x1234, y1234);
            splitPoint.SetIn(x123, y123);
            splitPoint.SetOut(x234, y234);

            return splitPoint;
        }

        // 곡선에 추가 포인트를 넣는 함수
        // 세 점을 기준으로 각도가 t_angle보다 작으면 가운데 점의 좌우 t만큼의 지점에 추가로 Point를 넣는다.
        // 급격하게 선이 꺾일 때 두께가 이상하게 나오는 현상을 감소시킬 수 있다.
        private void GetSplitPoints(List<DrawablePoint> pts, float t, float t_angle)
        {
            int count = pts.Count;

            if (count < 3)
                return;

            float t2 = 1.0f - t;

            int i = 1;  // 첫번째 가운데 점

            while (i < pts.Count - 1)
            {
                float angle = GetAngle(pts[i - 1], pts[i], pts[i + 1]);

                if (angle < t_angle)
                {
                    {   // 앞쪽에 추가되는 컨트롤 포인트
                        DrawablePoint point12 = GetPoint12(pts[i - 1], t2);
                        DrawablePoint point34 = GetPoint34(pts[i], t2);
                        DrawablePoint splitPoint = GetSplitPoint(pts[i - 1], pts[i], t2);
                        float d12 = splitPoint.GetDistance(pts[i - 1]);
                        float d23 = splitPoint.GetDistance(pts[i]);
                        float d123 = d12 + d23;
                        float force = pts[i - 1].Force + (pts[i].Force - pts[i - 1].Force) * (d12 / d123);

                        splitPoint.Force = force;

                        pts[i - 1].SetOut(point12.X, point12.Y);
                        pts.Insert(i, splitPoint);
                        pts[i + 1].SetIn(point34.X, point34.Y);
                    }

                    {   // 뒤쪽에 추가되는 컨트롤 포인트
                        DrawablePoint point12 = GetPoint12(pts[i + 1], t);
                        DrawablePoint point34 = GetPoint34(pts[i + 2], t);
                        DrawablePoint splitPoint = GetSplitPoint(pts[i + 1], pts[i + 2], t);
                        float d12 = splitPoint.GetDistance(pts[i + 1]);
                        float d23 = splitPoint.GetDistance(pts[i + 2]);
                        float d123 = d12 + d23;
                        float force = pts[i + 1].Force + (pts[i + 2].Force - pts[i + 1].Force) * (d12 / d123);

                        splitPoint.Force = force;

                        pts[i + 1].SetOut(point12.X, point12.Y);
                        pts.Insert(i + 2, splitPoint);
                        pts[i + 3].SetIn(point34.X, point34.Y);
                    }
                    i += 2;

                }
                else
                    i++;
            }
        }

        // 가까운 점을 제거하고 곡선이 아닌 라인상 거리를 재서 점을 없앨지 판단하는 로직
        // O(n)으로 처리하기 위해 복잡한 계산을 피하고 그 대신 결과물이 완벽하지 않을 때도 있다.
        private int IsNear(DrawablePoint p1, DrawablePoint p2, DrawablePoint p3, float tp, float tl)
        {
            float l12 = p1.GetDistance(p2);
            float l13 = p1.GetDistance(p3);
            float l23 = p2.GetDistance(p3);

            if (l12 < tp)
                return 2;

            if (l13 < tp)
                return 3;

            if (l23 < tp)
                return 2;

            float prj = ((p3.X - p1.X) * (p2.X - p1.X) + (p3.Y - p1.Y) * (p2.Y - p1.Y)) / l12;
            float d = 0;
            if (prj < 0)
            {
                d = p1.GetDistance(p3);
                if (d < tp)
                    return 1; // remove p1
            }
            else if (prj > l12)
            {
                d = p2.GetDistance(p3);
                if (d < tp)
                    return 2; // remove p2
            }
            else
            {
                float area = Math.Abs((p1.X - p3.X) * (p2.Y - p3.Y) - (p1.Y - p3.Y) * (p2.X - p3.X));
                d = area / l12;
                if (p1.GetDistance(p3) < tl || p2.GetDistance(p3) < tl || d < tl / 2)
                    return 3; // remove p3
            }

            return 0;
        }

        private void Simplify(List<DrawablePoint> pts, float maxThickness)
        {
            int midIndex = 0;
            float forceTemp;
            while (midIndex < pts.Count - 2 && pts.Count > 2)
            {
                float t_point = 0;     // 가까운 점을 제거하기 위한 거리 값
                float t_line = 0;      // 라인에서 떨어진 값을 제거하기 위한 거리 값

                float unitScale = 1f;

                maxThickness *= unitScale;

                t_point = maxThickness / 2.0f;
                t_line = maxThickness / 10.0f;

                if (t_point == 0)
                    return;

                t_point *= unitScale;
                t_line *= unitScale;

                // 1, 3, 2 순서로 점을 입력한다
                int result = IsNear(pts[midIndex], pts[midIndex + 2], pts[midIndex + 1], t_point, t_line);

                switch (result)
                {
                    case 0:     // 지울 점이 없는 경우
                        midIndex++;
                        break;
                    case 1:     // 첫 점을 지움
                        forceTemp = pts[midIndex].Force > pts[midIndex + 1].Force
                                  ? pts[midIndex].Force : pts[midIndex + 1].Force;
                        pts[midIndex].Set(pts[midIndex + 1]);
                        pts[midIndex].Force = forceTemp;
                        filtered.Add(pts[midIndex + 1]);
                        pts.RemoveAt(midIndex + 1);
                        break;
                    case 2:     // 끝 점을 지움
                        forceTemp = pts[midIndex + 1].Force > pts[midIndex + 2].Force
                                  ? pts[midIndex + 1].Force : pts[midIndex + 2].Force;
                        pts[midIndex + 2].Set(pts[midIndex + 1]);
                        pts[midIndex + 2].Force = forceTemp;
                        filtered.Add(pts[midIndex + 1]);
                        pts.RemoveAt(midIndex + 1);
                        break;
                    case 3:     // 가운데 점을 지움
                        pts[midIndex].Force = pts[midIndex].Force > pts[midIndex + 1].Force
                                                    ? pts[midIndex].Force : pts[midIndex + 1].Force;
                        pts[midIndex + 2].Force = pts[midIndex + 1].Force > pts[midIndex + 2].Force
                        ? pts[midIndex + 1].Force : pts[midIndex + 2].Force;
                        filtered.Add(pts[midIndex + 1]);
                        pts.RemoveAt(midIndex + 1);
                        break;
                }
            }
        }

        private void RemoveTail(List<DrawablePoint> pts, float maxThickness)
        {
            float distTailMax = 0;

            float unitScale = 1f;

            maxThickness *= unitScale;
            distTailMax = maxThickness / 2;
            distTailMax *= unitScale;

            bool findFirstTail = true;       // 진행중 첫 꼬리를 더 찾을 필요 없어지만 false
            int firstTailPoint = 0;             // 첫 꼬리로 판단되는 지점의 index
            int lastTailPoint = 0;              // 마지막 꼬리로 판단되는 지점의 index
            float distFirstTail = 0;           // 측정중인 첫 꼬리의 길이
            float finalDistFirstTail = 0;      // 첫 꼬리의 확정적인 길이(꼬리를 찾을 경우에 값이 들어감)
            float distLastTail = 0;            // 측정중인 마지막 꼬리의 길이
            float lastTailAngle = 0;           // 마지막 꼬리를 찾았을때 꼬리의 각도

            for (int i = 0; i < pts.Count - 2; ++i)
            {
                float angle = Math.Abs(GetAngle(pts[i], pts[i + 1], pts[i + 2]));
                distLastTail += pts[i + 1].GetDistance(pts[i + 2]);

                if (findFirstTail)
                    distFirstTail += pts[i].GetDistance(pts[i + 1]);

                if (angle < 90)
                {
                    lastTailPoint = i + 1;
                    distLastTail = pts[i + 1].GetDistance(pts[i + 2]);
                    lastTailAngle = angle;

                    if (distFirstTail >= (180.0 - angle) / 90.0 * distTailMax)
                        findFirstTail = false;
                    else if (findFirstTail)
                    {
                        firstTailPoint = i + 1;
                        finalDistFirstTail = distFirstTail;
                    }
                }
            }

            // 첫 꼬리 제거
            if (firstTailPoint != 0)
            {
                if (finalDistFirstTail > 0 && firstTailPoint < pts.Count)
                {
                    for (int i = 0; i < firstTailPoint; ++i)
                        pts.RemoveAt(0);
                    lastTailPoint -= firstTailPoint;
                }
            }

            // 마지막 꼬리 제거
            if (lastTailPoint != 0)
            {
                if (distLastTail < (180.0 - lastTailAngle) / 90.0 * distTailMax && lastTailPoint < pts.Count)
                {
                    for (int i = lastTailPoint; i < pts.Count; ++i)
                        pts.RemoveAt(pts.Count - 1);
                }
                //pts = pts.subList( 0, lastTailPoint + 1 );
            }
        }

        public void Dispose()
        {
            points?.Clear();
            midPoints?.Clear();
            leftPoints?.Clear();
            rightPoints?.Clear();
            filtered?.Clear();
            points = midPoints = leftPoints = rightPoints = filtered = null;
            CanvasCachedGeometry?.Dispose();
            CanvasCachedGeometry = null;
        }
    }
}
