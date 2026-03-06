using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics_Simulation__Create_Task_
{
    public struct Vector2
    {
        // X component of the vector.
        public float x;
        // Y component of the vector.
        public float y;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public const float EPSILON = 1e-6f; // standard epsilon value
        static readonly Vector2 zero = new Vector2(0, 0);
        public float Magnitude()
        {
            // Gets the magnitude of this vector
            // Needed for normalizing a vector to get directions
            return (float)Math.Sqrt((x * x) + (y * y));
        }
        public Vector2 Normalize()
        {
            // Normalize the vector
            // Essential for getting the direction of/between vectors
            float mag = Magnitude();

            // Epsilon is needed to prevent "divide by 0" errors
            if (mag > EPSILON)
                this /= mag;
            else
                this = zero;

            return this;
        }
        public static float Distance(Vector2 a, Vector2 b)
        {
            // Gets the distance between 2 vectors.
            // Needed for collision checks
            float diff_x = a.x - b.x;
            float diff_y = a.y - b.y;
            return (float)Math.Sqrt(diff_x * diff_x + diff_y * diff_y);
        }
        public static float Dot(Vector2 lhs, Vector2 rhs)
        {
            // Gets how much two vectors point at each other
            // Needed fro vector projection, which is important for collision response (relatie velocity)
            return lhs.x * rhs.x + lhs.y * rhs.y;
        }
        // Gets a random Vector2 between the x & y values of two vectors
        // This assumes that both components of v1 are less than v2
        public static Vector2 RandomRange(Vector2 v1, Vector2 v2)
        {
            float x = v1.x + Random.Shared.NextSingle() * (v2.x - v1.x);
            float y = v1.y + Random.Shared.NextSingle() * (v2.y - v1.y);
            return new Vector2(x, y);
        }

        // Operation implementation to make math easier and shorter.
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.y, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
        public static Vector2 operator *(float s, Vector2 a) => a * s;
        public static Vector2 operator /(Vector2 a, float s) => new Vector2(a.x / s, a.y / s);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.y && a.x == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
    }
}