namespace Physics_Simulation__Create_Task_
{
    public partial class Form1 : Form
    {
        const int MAX_BALLS = 100;
        const int MAX_BLOCKS = 15;

        // I was going to use an Object Pool (array of stored instances), but since the objects are so small, its redundent
        public List<Ball> balls = new List<Ball>();     
        private List<Block> blocks = new List<Block>();
        private Ball dummyBall = new Ball(); // Used for drawing a ball preview before spawning one, AND checking for valid spawns

        private Vector2 ballLaunchVelocity = new Vector2(5,5); // Velocity balls get when spawned
        private Vector2 lastMousePos = new Vector2(0,0); // The mouse position last frame. Used fro getting mouse velocity
        private Vector2 lastClickPos = new Vector2(0,0); // The mouse position when it last clicked. Used for sizing blocks;

        // Configurations for the next ball to be spawned
        float nextRadius = 0;
        float nextMaxx = 0;
       
        public Form1()
        {
            InitializeComponent();
        }
        public void Tick(float dt){

            ProcessBalls(dt);

           // lastMousePos = GetMousePos??; I'll figure this out when i get home
        }
        public void ProcessBalls(float dt) {
            // Process the active balls to make them move and collide
            foreach (Ball ball in balls) {
                // Move them first before checking collision, necessary fro accurate collision)
                ball.Move(dt);

                // Check Collision with walls first before balls (arbitrary order I chose)
                foreach (Block block in blocks) {
                    if (ball.Collides(block)) 
                        ball.ResolveCollision(block);
                }

                // Check collision between other balls last
                foreach (Ball ball2 in balls) {
                    // Check if (i != j) so the ball wont collide with itself
                    if (ball != ball2 && ball.Collides(ball2))
                        ball.ResolveCollision(ball2);
                }
            }
        }
        public void SpawnBall(Vector2 mousePos) {
            // Called on mouse press and spawns on the mouse
            // Return if there are no Balls left
            if (!IsAvailableBall() || !IsValidBallSpawnPosition()) return;
            
            Vector2 velocity;
            if (!useMouseVelocity) velocity = ballLaunchVelocity;
            else velocity = mousePos - lastMousePos;

            balls.Add(new Ball(mousePos, velocity, newRadius, newMass));
        }
        public bool IsAvailableBall() => balls.Count < MAX_BALLS;
        public bool IsValidBallSpawnPosition() {
            // Make sure a ball isn't inside a block or out of bounds before spawning it
            foreach (Block block in blocks)
            {
                if (dummyBall.Collides(block))
                    return true;
            }

            return false;
        }

        // Blocks
        public void SpawnBlock(Vector2 mousePos) {
            if (!isAvailableBlock()) return;
            
            // Size is the absolute distance between click and release
            Vector2 size = new Vector2(
                Math.Abs(lastClickPos.x - mousePos.x),
                Math.Abs(lastClickPos.y - mousePos.y)
            );
        
            // Position is the midpoint between the two points
            Vector2 position = (lastClickPos + mousePos) * 0.5f;

            blocks.Add(new Block(position, size));
        }
        public bool isAvailableBlock() => blocks.Count < MAX_BLOCKS;

        protected override void OnPaint(PaintEventArgs e)
        {

            foreach (Ball ball in balls)
            {
                e.Graphics.FillEllipse(Brushes.Blue, ball.position.x - ball.radius, ball.position.y - ball.radius,
                    ball.radius * 2, ball.radius * 2);
            }
            
        }
    }

    // Settings of the game, which includes stats for objects.
    public static class GameConfig {
        // Settings for balls
        public static float Gravity = 3.0f;
        public static float AirDrag = 2.5f;
        public static bool UseMouseVelocity = false;
        public static int MaxBalls { get; private set; } = 100;
        public static int MaxBlocks { get; private set; } = 15;
    }
}
