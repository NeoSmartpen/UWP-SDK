using Neosmartpen.Net;
using System;

namespace SampleApp
{
    public class DrawablePoint
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Force { get; set; }

        public float InX { get; internal set; }

        public float InY { get; internal set; }

        public float OutX { get; internal set; }

        public float OutY { get; internal set; }

        public float MaxForce { get; set; } = 1024f;

        public DrawablePoint()
        {
            X = Y = InX = InY = OutX = OutY = Force = 0;
        }

        public DrawablePoint(Dot dot)
        {
            X = dot.X * 56f;
            Y = dot.Y * 56f;
            Force = dot.Force / MaxForce;
        }

        public DrawablePoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public DrawablePoint(float x, float y, float force)
        {
            X = x;
            Y = y;
            Force = force;
        }

        public DrawablePoint(DrawablePoint p)
        {
            X = p.X;
            Y = p.Y;
            Force = p.Force;
        }

        public DrawablePoint(DrawablePoint p, float scale) : this(p)
        {
            SetScale(scale);
        }

        public void Set(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void Set(float x, float y, float force)
        {
            X = x;
            Y = y;
            Force = force;
        }

        public void Set(DrawablePoint p)
        {
            X = p.X;
            Y = p.Y;
            Force = p.Force;
        }

        public void SetIn(float x, float y)
        {
            InX = x;
            InY = y;
        }

        public void SetOut(float x, float y)
        {
            OutX = x;
            OutY = y;
        }

        public float GetDistance(DrawablePoint p)
        {
            float dx = p.X - X;
            float dy = p.Y - Y;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public float GetDistanceInOut()
        {
            float dx = InX - OutX;
            float dy = InY - OutY;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public void SetScale(float scale)
        {
            X *= scale;
            Y *= scale;
            InX *= scale;
            InY *= scale;
            OutX *= scale;
            OutY *= scale;
        }
    }
}
