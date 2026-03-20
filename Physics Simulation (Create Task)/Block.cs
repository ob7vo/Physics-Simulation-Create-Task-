using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics_Simulation__Create_Task_
{
    public class Block
    {
        static readonly float[] MIN_MAX_FRICTION_COEFFICIENT = { 0.05f, 0.95f };

        public Vector2 position = new Vector2(); 
        public Vector2 size = new Vector2();
        public Rectangle rect = new Rectangle(); // Cached rect to avoid making one every frame to draw it
        
        public float frictionCoefficient = 0.0f;

        public readonly Color color = Color.White;

        public Block(Vector2 pos, Vector2 size)
        {
            this.position = pos;
            this.size = size;
            this.frictionCoefficient = Utility.RandomLerp(MIN_MAX_FRICTION_COEFFICIENT);

            rect = new Rectangle((int)Math.Round(Left), (int)Math.Round(Top), (int)Math.Round(size.x), (int)Math.Round(size.y));
            color = Utility.RandomizeColor();
        }
        // This construtcotr is purely for the previewBlock
        public Block() => color = Color.FromArgb(100, 255, 255, 255);
        
        public void SetRect() => rect = new Rectangle((int)Math.Round(Left), (int)Math.Round(Top), (int)Math.Round(size.x), (int)Math.Round(size.y));
        public void SetRectFromMousePosition(Vector2 lastClickPos, Vector2 mousePos)
        {
            size = (lastClickPos - mousePos).Abs();
            position = (lastClickPos + mousePos) * 0.5f;
            SetRect();
        }

        public bool Collides(Vector2 point)
        {
            return Bottom >= point.y &&
                Top <= point.y &&
                Right >= point.x &&
                Left <= point.x;
        }
        public bool Collides(Block block2) {
            return Bottom >= block2.Top &&
                Top <= block2.Bottom &&
                Right >= block2.Left &&
                Left <= block2.Right;
        }
        public Vector2 GetContactPoints(Ball ball) {
            return new Vector2(
                Math.Clamp(ball.position.x, Left, Right),
                Math.Clamp(ball.position.y, Top, Bottom)
            );
        }
        public Vector2 GetCollisionNormal(Block block, Vector2 contactPoint)
        {
            // If contact point is on top or bottom, normal is vertical
            // If contact point is on left or right, normal is horizontal
            Vector2 diff = position - contactPoint;

            if (Math.Abs(diff.x) > Math.Abs(diff.y))
                return new Vector2(Math.Sign(diff.x), 0); // left or right wall
            else
                return new Vector2(0, Math.Sign(diff.y)); // top or bottom
        }

        public float Top => position.y - (size.y * 0.5f);
        public float Bottom => position.y + (size.y * 0.5f);
        public float Left => position.x - (size.x * 0.5f);
        public float Right => position.x + (size.x * 0.5f);
        public float HalfWidth => size.x * 0.5f;
        public float HalfHeight => size.y * 0.5f;

    }
}
