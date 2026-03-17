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
        static readonly float[] MIN_MAX_MASS = { 1.0f, 2.5f };
        static readonly float[] MIN_MAX_RESTITUTION = { 0.9f, 1.0f };

        public Vector2 position = new Vector2(0, 0);
        public Vector2 velocity = new Vector2(0,0);

        int ticks = 0;

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
        public void ResolveCollision(Block block)
        {
        //    FixOverlap(block);

            Vector2 contactPoint = block.GetContactPoints(this);
            Vector2 normal = (position - contanctPoint).Normalize();

            // Reflect only along the normal axis
            float sepVel = Vector2.Dot(velocity, normal);
            if (sepVel < 0) // only resolve if moving toward the block
                velocity -= (1 + restitution) * sepVel * normal;


            // Set grounded state and fix overlaps afterwards
            CheckIfGrounded(block);
            FixOverlap(block);
        }
        public void ResolveCollision(Ball ball2)
        {
            // Collision is resolved via Elastic Collision (both move in opposite directions)
            // Get the direction between the balls
            Vector2 normal = (ball2.position - this.position);
            normal.Normalize();

            // The velocity of this ball in reference to teh other
            Vector2 relVel = this.velocity - ball2.velocity;
            // Only the speed on the axis of direction is needed
            // Using Dot product projects relative velocity onto the normal
            float seperationVelocity = Vector2.Dot(relVel, normal);

            // Speed being positive means the balls are moving away from each other
            // Resolving the collision then would just pull them back
            if (seperationVelocity > 0) return;

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
        public void FixOverlap(Ball ball2, Vector2 normal)
        {
            // Remove any overlap between the two
            float overlap = (this.radius + ball2.radius) - Vector2.Distance(this.position, ball2.position);

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
