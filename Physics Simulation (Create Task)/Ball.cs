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
        static readonly float[] MIN_MAX_RADIUS = { 10.0f, 20.0f };
        static readonly float[] MIN_MAX_MASS = { 1.0f, 2.5f }; // Set low so its more reliant on radius
        static readonly float[] MIN_MAX_RESTITUTION = { 0.3f, 0.5f };

        public Vector2 position = new Vector2(0, 0);
        public Vector2 velocity = new Vector2(0,0);

        public float radius { get; private set; } = 5.0f;
        public float mass { get; private set; } = 3.0f;
        public readonly float restitution = 1.0f; // Coefficient of Restitution

        public bool grounded = false;
        private float curFrictionCoefficient = 0.0f; // The "Mu" value of the block the ball is currently on

        public readonly Color color = Color.White;
        
        public Ball(Vector2 position, Vector2 velocity)
        {
            this.position = position;
            this.velocity = velocity;

            this.radius = Utility.RandomLerp(MIN_MAX_RADIUS);
            this.mass = radius * Utility.RandomLerp(MIN_MAX_MASS);
            this.restitution = Utility.RandomLerp(MIN_MAX_RESTITUTION);

            grounded = false;
            color = Utility.RandomizeColor();
        }
        public Ball() { }

        public void Move(float deltaTime)
        {
            // DeltaTime is essential to have consistency regardless of framerate
            // DeltaTime is used fro bth acceleration and adding velocity, since these happen OVER TIME

            // Mass isn't needed for gravity acceleration since its cancelled out
            // Fg = mg -> a = F / m -> a = (m)g / (m) -> a = g.
            if (!grounded) {
                velocity.y += (GameConfig.Gravity * deltaTime);
                velocity -= velocity * GameConfig.AirDrag * deltaTime; // Very simplified Air drag formula
            } 
            else {
                // Apply friction when grounded
                // Force of gravity equation. Mass gets cancelled out. ((mu * m * g) / m = a)
                float frictionDecel = curFrictionCoefficient * GameConfig.Gravity * deltaTime;

                // Use absolute value and minimize at 0 friction doesn't reverse the ball's direction
                if (Math.Abs(velocity.x) <= frictionDecel)
                    velocity.x = 0;
                else
                    velocity.x -= Math.Sign(velocity.x) * frictionDecel; // Get the Sign so it always deccelerate in the opposite direction
            }

            position += velocity * deltaTime;

            //Debug.WriteLine(velocity.ToString() + " -> Velocity");
            //Debug.WriteLine(position.ToString() + " -> Position");
        }
        public bool Collides(Vector2 point) => Vector2.Distance(this.position, point) <= this.radius;
        public bool Collides(Ball ball2)
        {
            return Vector2.Distance(this.position, ball2.position) <= (this.radius + ball2.radius);
        }
        public bool Collides(Block block)
        { 
            // Get the sides the the circle is closest to.
            float closestX = Math.Clamp(position.x, block.Left, block.Right);
            float closestY = Math.Clamp(position.y, block.Top, block.Bottom);

            float distX = position.x - closestX;
            float distY = position.y - closestY;

            // Distance check but without the Math.Sqrt() so it's faster
            return (distX * distX + distY * distY) <= (radius * radius);
        }
        public bool Collides(Rectangle screenBounds)
        {
            return Left < screenBounds.Left || Right > screenBounds.Right ||
                Top < screenBounds.Top || Bottom > screenBounds.Bottom;
        }
        public void ResolveCollision(Block block)
        {
            Vector2 oldVel = velocity; // To write on Debug
           
            Vector2 contactPoint = block.GetContactPoints(this);
            Vector2 normal = (position - contactPoint).Normalize();

            // Use Relative Velocity Reversal theorem.
            float sepVel = Vector2.Dot(velocity, normal);
            if (sepVel < 0) // only resolve if moving toward the block
                velocity -= (1 + restitution) * sepVel * normal;

          //  Debug.WriteLine(oldVel.ToString() + " -> Old Velocity -----" +
           //     velocity.ToString() + " -> New Velocity");

            // Set grounded state and fix overlaps afterwards
            CheckIfGrounded(block);
            FixOverlap(block);
        }
        public void ResolveCollision(Ball ball2)
        {
            // Collision is resolved via Elastic Collision (both move in opposite directions)
            // Get the direction between the balls
            Vector2 normal = (this.position - ball2.position);
            normal.Normalize();

            // The velocity of this ball in reference to teh other
            Vector2 relVel = this.velocity - ball2.velocity;
            // Only the speed on the axis of direction is needed
            // Using Dot product projects relative velocity onto the normal
            float seperationVelocity = Vector2.Dot(relVel, normal);

            // Speed being positive means the balls are moving away from each other
            // Resolving the collision then would just pull them back
            if (seperationVelocity > 0)
            {
               // Debug.WriteLine("Moving AWAY");
                return;
            }

            // Average the elasticity between the two (could do min(), but I like this more)
            float elasticity = (this.restitution + ball2.restitution) * 0.5f;

            // The final velocities are found using both Conservation of Momentum and Relative Velocity Reversal equations
            // CoM Formula: m1v1 + m2v2 = m1v1' + m2v2'
            // RVR Formula: v1 - v2 = -(v1'-v2')
            // Solve: Delta(v1 or v2)* = V1(m1-m2)+((2*m(1or2)*V(1or2))
            //                           ------------------------------ * ((v1 - v2) * n) * n))
            //                                      (m1+m2))                                   
            // n is the normal direction, and ((v1 - v2) * n) * n)) is the speed variable.
            // the 2 is (1 + e) where e = 1, which would be perfectly elastic. Adding restitution allows the value to change

            // Instead of just inputting the equation into both velocities, chaning them to DELTAVelocity equations and
            // creating this impulse float will save some computations, making this all faster
            float impulse = ((1 + elasticity) * seperationVelocity) / (this.mass + ball2.mass);

            // Add and Substract the different results so that the balls go in opposite directions
            this.velocity -= normal * (impulse * ball2.mass);
            ball2.velocity += normal * (impulse * this.mass);

            // The balls will be overlapping, which would cause problems
            FixOverlap(ball2, normal);
        }
        public void ResolveCollision(Rectangle screenBounds)
        {
            if (GameConfig.BallBorderBehavior == BorderBehavior.Wrap)
            {
                // When wrapping the ball, I move it by some of its velocity to avoid it immediately wrapping-around again
                if (Left < screenBounds.Left)
                    position.x = screenBounds.Right - radius;
                else if (Right > screenBounds.Right)
                    position.x = screenBounds.Left + radius;
                else if (Top < screenBounds.Top)
                    position.y = screenBounds.Bottom - radius;
                else if (Bottom > screenBounds.Bottom)
                    position.y = screenBounds.Top + radius;
            }
            // Bounce
            else
            {
                Vector2 contactPoint = new Vector2(
                    Math.Clamp(position.x, screenBounds.Left, screenBounds.Right),
                    Math.Clamp(position.y, screenBounds.Top, screenBounds.Bottom)
                );
                Vector2 normal = (position - contactPoint).Normalize();

                // Use Relative Velocity Reversal theorem.
                float sepVel = Vector2.Dot(velocity, normal);
                if (sepVel < 0) // only resolve if moving toward the block
                    velocity -= (1 + restitution) * sepVel * normal;

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
            
            this.position -= normal * (overlap * 0.5f);
            ball2.position += normal * (overlap * 0.5f);
        }
        public void FixOverlap(Block block)
        {
            float overlapX = (radius + block.HalfWidth) - Math.Abs(position.x - block.position.x);
            float overlapY = (radius + block.HalfHeight) - Math.Abs(position.y - block.position.y);

            if (overlapX < overlapY)
                position.x += overlapX * Math.Sign(position.x - block.position.x);
            else
                position.y += overlapY * Math.Sign(position.y - block.position.y);
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

        public float Top => position.y - radius;
        public float Bottom => position.y + radius;
        public float Left => position.x - radius;
        public float Right => position.x + radius;
    }
}
