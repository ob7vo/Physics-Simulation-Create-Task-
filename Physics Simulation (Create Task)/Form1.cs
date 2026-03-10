namespace Physics_Simulation__Create_Task_
{
    public partial class Form1 : Form
    {
        const int MAX_BALLS = 100;
        const int MAX_BLOCKS = 15;

        // I was going to use an Object Pool (array of stored instances), but since the objects are so small, its redundent
        private List<Ball> balls = new List<Ball>;        
        private List<Block> blocks = new List<Block>;

        private Vector2 ballLaunchVelocity = new Vector2(5,5); // Velocity balls get when spawned
        private Vector2 lastMousePos = new Vector2(0,0); // The mouse position last frame. Used fro getting mouse velocity
        private Vector2 lastClickPos = new Vector2(0,0); // The mouse position when it last clicked. Used for sizing blocks;
        private bool useMouseVelocity = false; // if true, the spawn velocity fro balls with use the mouse, and not ballLaunhVelocity
        
        public Form1()
        {
            InitializeComponent();
        }
        public void Tick(float dt){

            ProcessBalls(dt);
            Draw();

           // lastMoustPos = GetMousePos??; I'll figure this out when i get home
        }
        public void ProcessBalls(float dt) {
            // Process the active balls to make them move and collide
            for (int i = 0; i < MAX_BALLS; i++) {
                if (!activeBalls[i]) continue;

                // Move them first before checking collision, necessary fro accurate collision)
                balls[i].Move(dt);

                // Check Collision with walls first before balls (arbitrary order I chose)
                for (int j = 0; j < MAX_BLOCKS; j++) {
                    if (activeBlocks[j] && balls[i].Collides(blocks[j])
                        balls[i].HandleCollision(blocks[j]);
                }

                // Check collision between other balls last
                for (int j = 0; j < MAX_BALLS; j++) {
                    // Check if (i != j) so the ball wont collide with itself
                    if (activeBalls[j] && i != j && balls[i].Collides(balls[j]);
                        balls[i].HandleCollision(balls[j]);
                }
            }
        }
        public void SpawnBall(Vector2 mousePos, int newRadius, int newMass) {
            // Called on mouse press and spawns on the mouse
            // Return if there are no Balls left

            // Activate Balls will make sure there is a ball with a valid spawn
            int idx = ActivateBall(mousePos);
            if (idx == -1) return; // There are no balls available to spawn

            // Set the balls new stats before checking if its in a valid spawn (mass techinically doesnt serve that purpose)
            balls[idx].position = mousePos;
            balls[idx].radius = newRadius;
            balls[idx].mass = newMass;
            
            if (!useMouseVelocity) balls[idx].velocity = ballLaunchVelocity;
            else balls[idx].velocity = mousePos - lastMousePos;
        }
        public int ActivateBall() {
            // Finds an inactive ball to spawn in
            // If there are no inactive balls, it returns -1 so its known 
            // thats why I return an int and not a reference, I also just don't like pointers

            for (int i = 0; i < MAX_BALLS; i++) {
                if (!activeBalls[i]) {
                    activeBalls[i] = true;
                    return i;
                }
            }

            return -1;
        }
        public bool isValidBallSpawnPosition(Vector2 spawnPos) {
            // Make sure a ball isn't inside a block or out of bounds before spawning it
            
            for (int j = 0; j < MAX_BLOCKS; j++) {
                    if (activeBlocks[j] && balls[i].Collides(blocks[j])
                        balls[i].HandleCollision(blocks[j]);
                }
        }

        // Blocks
        public void SpawnBlock(Vector2 mousePos) {
            int idx = ActivateBlock();
            if (idx == -1) return;
            
            // Size is the absolute distance between click and release
            Vector2 size = new Vector2(
                Mathf.Abs(lastClickPos.x - mousePos.x),
                Mathf.Abs(lastClickPos.y - mousePos.y)
            );
        
            // Position is the midpoint between the two points
            Vector2 position = (lastClickPos + mousePos) * 0.5f;

            blocks[idx].position = position;
            blocks[idx].
        }
        public int ActivateBlock() {
            // Same as ActivateBall(), just for blocks

            for (int i = 0; i < BLOCKS; i++) {
                if (!activeBlocks[i]) {
                    activeBlocks[i] = true;
                    return i;
                }
            }
        }
        
        public void Draw() {

        }
    }
}
