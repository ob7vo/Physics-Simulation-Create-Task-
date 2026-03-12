using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics_Simulation__Create_Task_
{
    public class Block
    {
        public Vector2 position = new Vector2(); 
        public Vector2 size = new Vector2();

        public float frictionCoefficient = 0.f;
        
        public Block(Vector2 pos, Vector2 size, float frictionCoefficient)
        {
            this.position = pos;
            this.size = size;
            this.frictionCoefficient = frictionCoefficient;
        }

        public bool Collides(Block block2) {
            return position.y + size.y * 0.5f >= block2.position.y - block2.size.y * 0.5f &&
                position.y - size.y * 0.5f <= block2.position.y + block2.size.y * 0.5f &&
                position.y + size.x * 0.5f >= block2.position.y - block2.size.x * 0.5f &&
                position.y - size.x * 0.5f <= block2.position.y + block2.size.x * 0.5f;
        }
        public Vector2 GetContactPoints(Ball ball) {
            return new Vector2(
                Math.Clamp(ball.position.x, position.x - size.x * 0.5f, position.x + size.x * 0.5f),
                Math.Clamp(ball.position.y, position.y - size.y * 0.5f, position.y + size.y * 0.5f)
            );
        }
    }
}
