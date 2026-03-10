using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics_Simulation__Create_Task_
{
    //const float BALL_RADIUS = 3;

    internal class Ball
    {
        Vector2 position = new Vector2(0,0);
        Vector2 velocity = new Vector2(0,0);

        float radius = 5.f;
        float mass = 3.f;

        bool grounded = false;

        public Ball(Vector2 position, Vector2 velocity, float radius, float mass)
        {
            this.position = position;
            this.velocity = velocity;
            this.radius = radius;
            this.mass = mass;
        }

        public void Move(float deltaTime, float gravity)
        {
            // DeltaTime is essential to have consistency regardless of framerate
            // DeltaTime is used fro bth acceleration and adding velocity, since these happen OVER TIME

            // Mass isn't needed for gravity acceleration since its cancelled out
            // Fg = mg -> a = F / m -> a = (m)g / (m) -> a = g.
            velocity.y -= gravity * deltaTime;

            position += velocity * deltaTime;
        }
        public bool Collides(Ball ball2)
        {
            return Vector2.Distance(this.position, ball2.position) <= (this.radius + ball2.radius);
        }
        public bool Collides(Block block)
        {
            float halfW = block.size.x * 0.5f;
            float halfH = block.size.y * 0.5f;

            // Get the sides the the circle is closest to.
            float closestX = Math.Clamp(position.x, block.position.x - halfW, block.position.x + halfW);
            float closestY = Math.Clamp(position.y, block.position.y - halfH, block.position.y + halfH);

            float distX = position.x - closestX;
            float distY = position.y - closestY;

            // Distance check but without the Math.Sqrt() so it's faster
            return (distX * distX + distY * distY) <= (radius * radius);
        }
        public void ResolveCollision(Block block)
        {
            // I dont know how to get contact point
            Vector2 contactPoint = new Vector2(0,0);

            Vector2 direction = (position - contactPoint).Normalize();

            // Reflect the velocity of the ball. No velocity is lost
            if (Math.Sign(direction.x) != Math.Sign(velocity.x))
                velocity.x *= -1;
            if (Math.Sign(direction.y) != Math.Sign(velocity.y))
                velocity.y *= -1;
        }
        public void ResolveCollision(Ball ball2)
        {
            // Collision is resolved via Elastic Collision (both move in opposite directions)
            // Get the direction between the balls
            Vector2 normal = (ball2.position - this.position);
            normal.Normalize();

            // Relative Velocity and speed are needed to ge tthe final impulse
            Vector2 relVel = this.velocity - ball2.velocity;
            float speed = Vector2.Dot(relVel, normal);

            // Speed being positive means the balls are moving towards each other
            // Resolving the collision then would just pull them back
            if (speed > 0) return;

            // The balls may still be overlapping the next frame, which would cause jank
            // **Im not sure if this should go at the top or here just yet**
            FixOverlap(ball2, normal);

            float impulse = (2 * speed) / (this.mass + ball2.mass);

            // Add and Substract the different results so that the balls go in opposite directions
            this.velocity -= normal * (impulse * ball2.mass);
            ball2.velocity += normal * (impulse * this.mass);
        }
        public void FixOverlap(Ball ball2, Vector2 normal)
        {
            // Remove any overlap between the two
            float overlap = (this.radius + ball2.radius) - Vector2.Distance(this.position, ball2.position);

            this.position -= normal * (overlap * 0.5f);
            ball2.position += normal * (overlap * 0.5f);
        }
        public void FixOverlap(Block block)
        {
            //Push the ball out of the Block
            // This wont work since position is centered
            float halfWidth = block.size.x * 0.5f;
            float overlapX = (radius + position.x);
        }
    }
}
