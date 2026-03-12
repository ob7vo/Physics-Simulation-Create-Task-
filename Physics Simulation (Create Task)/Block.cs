using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics_Simulation__Create_Task_
{
    public class Block
    {
        public Vector2 position;
        public Vector2 size;

        public Block(Vector2 pos, Vector2 size)
        {
            this.position = pos;
            this.size = size;
        }
    }
}
