using System;

namespace Dq99.Prototype.Domain
{
    [Serializable]
    public struct Float2
    {
        public float X;
        public float Y;

        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Float2 Zero => new Float2(0f, 0f);

        public float Magnitude => (float)Math.Sqrt((X * X) + (Y * Y));

        public Float2 Normalized
        {
            get
            {
                var magnitude = Magnitude;
                return magnitude > 0.0001f ? new Float2(X / magnitude, Y / magnitude) : Zero;
            }
        }

        public static Float2 operator +(Float2 left, Float2 right) => new Float2(left.X + right.X, left.Y + right.Y);
        public static Float2 operator -(Float2 left, Float2 right) => new Float2(left.X - right.X, left.Y - right.Y);
        public static Float2 operator *(Float2 left, float scalar) => new Float2(left.X * scalar, left.Y * scalar);

        public static float Distance(Float2 a, Float2 b) => (a - b).Magnitude;
    }
}
