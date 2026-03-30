using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Physics_Simulation__Create_Task_
{
    //const float BALL_RADIUS = 3;

    public class Ball
    {
        const float SKIN_WIDTH = 0.025f;
        public static readonly float[] MIN_MAX_RADIUS = { 10.0f, 20.0f };
        static readonly float[] MIN_MAX_MASS = { 1.0f, 2.5f }; // Set low so its more reliant on radius
        static readonly float[] MIN_MAX_RESTITUTION = { 0.3f, 0.5f };

        // Instead of having a velocity Vector, I store the current adn last positions
        // I get velocity using Verlet Integration (Vel =  (Pos - lastPos))
        // I tried both, and Verlet Integration was far, far superior for accurate and consistent collisions
        public Vector2 position = new Vector2(0, 0);
        public Vector2 lastPosition = new Vector2(0,0);

        public float radius { get; private set; } = 5.0f;
        public float mass { get; private set; } = 3.0f;
        public readonly float restitution = 1.0f; // Coefficient of Restitution

        public bool grounded = false;
        private float curFrictionCoefficient = 0.0f; // The "Mu" value of the block the ball is currently on

        public readonly Color color = Color.White;
        
        public Ball(Vector2 position, Vector2 velocity, float radius)
        {
            this.position = position;
            this.lastPosition = position - velocity * GameConfig.FixedDeltaTime;

            this.radius = radius;
            this.mass = radius * Utility.RandomLerp(MIN_MAX_MASS);
            this.restitution = Utility.RandomLerp(MIN_MAX_RESTITUTION);

            grounded = false;
            color = Utility.RandomizeColor();
        }
        public Ball() { }

        public void Move(float deltaTime)
        {
            Vector2 oldPosition = position;
            // DeltaTime is essential to have consistency regardless of framerate
            // DeltaTime is used fro bth acceleration and adding velocity, since these happen OVER TIME

            // Mass isn't needed for gravity acceleration since its cancelled out
            // Combine simplified AirDrag and Gravity into one line
            if (!grounded) {
             //   Debug.WriteLine("POS - " + position.ToString());
                position += Velocity + (GameConfig.Gravity - Velocity * GameConfig.AirDrag) * (deltaTime * deltaTime);
             //   Debug.WriteLine("POS AFTER GRAVITY - " + position.ToString());
            } 
            else {
                // Applying friction
                Vector2 implicitVelocity = Velocity;
                // Equation for acceleration from Force of Friction (Mass is cancelled out again)
                float frictionDecel = curFrictionCoefficient * GameConfig.Gravity.y * deltaTime * deltaTime;

                // Set the velocity to 0 when approaching small values.
                if (Math.Abs(implicitVelocity.x) <= frictionDecel)
                    implicitVelocity.x = 0;
                else // Reduce the velocity by the acceleration of friction (I use Sign so it wont reverse)
                    implicitVelocity.x -= Math.Sign(implicitVelocity.x) * frictionDecel;

                position += implicitVelocity; // No acceleration when grounded
            }

            lastPosition = oldPosition;
        }
        public bool SolveCollision(Ball ball2){
            const float EPSILON = 1e-6f; // standard epsilon value

            Vector2 dir = this.position - ball2.position;
            float sqrDist = dir.x * dir.x + dir.y * dir.y;

            float totalRadius = this.radius + ball2.radius;
            if (sqrDist <= totalRadius * totalRadius && sqrDist > EPSILON)
            {
                float dist = (float)Math.Sqrt(sqrDist);
                float push = (totalRadius - dist) * 0.5f;
                float res = (this.restitution + ball2.restitution) * 0.5f;

                // normal * (1/2 overlap) * restitution
                Vector2 collisionVelocity = (dir / dist) * push * res;

                this.position += collisionVelocity;
                ball2.position -= collisionVelocity;

                return true;
            }

            return false;
        }
        public bool SolveCollision(Block block)
        {
            // Get the sides the the circle is closest to.
            Vector2 contactPoints = new Vector2(
                Math.Clamp(position.x, block.Left, block.Right),
                Math.Clamp(position.y, block.Top, block.Bottom)
            );
            Vector2 dist = position - contactPoints;

            // Distance check but without the Math.Sqrt() so it's faster
            if (dist.SqrMagnitude <= (radius * radius)) {
                Vector2 normal = dist.Normalize();

                if (Math.Abs(normal.x) > Math.Abs(normal.y))
                    normal = new Vector2(Math.Sign(normal.x), 0);
                else
                    normal = new Vector2(0, Math.Sign(normal.y));

                // Use Relative Velocity Reversal theorem. 
                float sepVel = Vector2.Dot(Velocity, normal);
                if (sepVel < 0) // only resolve if moving toward the block
                    lastPosition = position - (Velocity - ((1 + restitution) * sepVel * normal));

                // Set grounded state and fix overlaps afterwards
                CheckIfGrounded(block);
                FixOverlap(block);

                return true;
            }

            return false;
        }
        public bool Collides(Rectangle screenBounds)
        {
            return Left < screenBounds.Left || Right > screenBounds.Right ||
                Top < screenBounds.Top || Bottom > screenBounds.Bottom;
        }
        public void ResolveCollision(Rectangle screenBounds)
        {
            // Wrap around
            if (GameConfig.BallBorderBehavior == BorderBehavior.Wrap)
            {
                // Velocity when at the other side of the wall
                Vector2 oldVelocity = Velocity;

                // When wrapping the ball, I move it by some of its velocity to avoid it immediately wrapping-around again
                if (Left < screenBounds.Left)
                    position.x = screenBounds.Right - radius;
                else if (Right > screenBounds.Right)
                    position.x = screenBounds.Left + radius;
                else if (Top < screenBounds.Top)
                    position.y = screenBounds.Bottom - radius;
                else if (Bottom > screenBounds.Bottom)
                    position.y = screenBounds.Top + radius;

                // Old Velocity is used here so the ball wont get insane Velocity value when wrapped to the other side
                lastPosition = position - oldVelocity;
            }
            // Bounce
            else
            {
                Vector2 vel = Velocity;

                if (Left < screenBounds.Left || Right > screenBounds.Right)
                   vel.x *= -restitution;

                if (Top < screenBounds.Top || Bottom > screenBounds.Bottom)
                    vel.y *= -restitution;

                lastPosition = position - vel;
                FixOverlap(screenBounds);
            }
  
        }
        public void FixOverlap(Ball ball2, Vector2 normal)
        {
            // Remove any overlap between the two
            float totalRadius = this.radius + ball2.radius;
            float overlap = (totalRadius) - Vector2.Distance(this.position, ball2.position);

            // I create ratios so that teh balls are move accordingly to their size
            // Bigger balls will have to move out less
            // float ratio1 = this.radius / totalRadius;
            // float ratio2 = 1 - ratio1;

            Vector2 adjustment = (normal * (overlap * 0.5f)).Add(SKIN_WIDTH);
            this.position -= adjustment;
            ball2.position += adjustment;
        }
        public void FixOverlap(Block block)
        {
            float overlapX = (radius + block.HalfWidth) - Math.Abs(position.x - block.position.x);
            float overlapY = (radius + block.HalfHeight) - Math.Abs(position.y - block.position.y);

            if (overlapX < overlapY)
                position.x += (overlapX * Math.Sign(position.x - block.position.x)) + SKIN_WIDTH;
            else
                position.y += (overlapY * Math.Sign(position.y - block.position.y)) + SKIN_WIDTH;
        }
        public void FixOverlap(Rectangle screenBounds)
        {
            if (Left < screenBounds.Left)
                position.x = screenBounds.Left + (radius + SKIN_WIDTH);
            else if (Right > screenBounds.Right)
                position.x = screenBounds.Right - (radius + SKIN_WIDTH);

            if (Top < screenBounds.Top)
                position.y = screenBounds.Top + (radius + SKIN_WIDTH);
            else if (Bottom > screenBounds.Bottom)
                position.y = screenBounds.Bottom - (radius + SKIN_WIDTH);
        }
        public bool CheckIfGrounded(Block block)
        {
            if (Bottom - SKIN_WIDTH > block.Top)
            {
                curFrictionCoefficient = block.frictionCoefficient;
                return grounded = true;
            }
            else
            {
                curFrictionCoefficient = 0;
                return grounded = false;
            }
        }

        public Vector2 Velocity => position - lastPosition;
        public float Top => position.y - radius;
        public float Bottom => position.y + radius;
        public float Left => position.x - radius;
        public float Right => position.x + radius;
    }
}
