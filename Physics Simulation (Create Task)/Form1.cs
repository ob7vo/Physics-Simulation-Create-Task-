using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Timers;

namespace Physics_Simulation__Create_Task_
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        private Stopwatch stopwatch = new Stopwatch();
        Rectangle screenBounds;
        UI ui;

        const int MAX_BALLS = 100;
        const int MAX_BLOCKS = 15;

        // I was going to use an Object Pool (array of stored instances), but since the objects are so small, its redundent
        public List<Ball> balls = new List<Ball>();     
        private List<Block> blocks = new List<Block>();
        private Ball previewBall = new Ball(); // Used for drawing a ball preview before spawning one, AND checking for valid spawns
        private Block previewBlock = new Block();
        
        private Vector2 ballLaunchVelocity = new Vector2(5,5); // Velocity balls get when
        bool holdingMouse = false;
        private Vector2 lastMousePos = new Vector2(0,0); // The mouse position last frame. Used fro getting mouse velocity
        private Vector2 lastClickPos = new Vector2(0,0); // The mouse position when it last clicked. Used for sizing blocks;

        private SpawnNext nextSpawn = SpawnNext.Block;
        float ballSpawnTime = 0.0f;
        float physicsTickTime = 0.0f;
        bool autoSpawnBalls = false;

        private readonly float FixedDeltaTime = 0.2f;

        public Form1()
        {
            InitializeComponent();

            FixedDeltaTime = GameConfig.PhysicsFixedTickTimer / GameConfig.PhysicsSubsteps;

            stopwatch.Start();

            Application.Idle += Update;
            MouseDown += OnMousePress;
            MouseUp += OnMouseRelease;
            KeyDown += OnKeyPress;

            screenBounds = this.ClientRectangle;
            this.DoubleBuffered = true; // Fixes white frame flashes
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |   // Skip WM_ERASEBKGND message
                ControlStyles.UserPaint |              // We handle painting
                ControlStyles.OptimizedDoubleBuffer,   // Off-screen buffer
                true
            );
            this.UpdateStyles();
            this.BackColor = Color.Black;

            ui = new UI(screenBounds);
            ui.SetVelocityText(ballLaunchVelocity);

            previewBlock = new Block();
            TestStart();
        }

        private void TestStart()
        {
            // Spawn a block
            Vector2 position = new Vector2(
                screenBounds.X + screenBounds.Width * 0.5f,
                screenBounds.Y + screenBounds.Height * 0.9f
                );

            Vector2 size = new Vector2(
                screenBounds.Width * 0.4f,
                screenBounds.Height * 0.1f
                );

            blocks.Add(new Block(position, size));

            Vector2 position1 = new Vector2(
                screenBounds.X + screenBounds.Width * 0.55f,
                screenBounds.Y + screenBounds.Height * 0.7f
                );
            Vector2 position2 = new Vector2(
                screenBounds.X + screenBounds.Width * 0.7f,
                screenBounds.Y + screenBounds.Height * 0.7f
                );
            // Spawn a ball
            balls.Add(new Ball(position1, new Vector2(50.0f,0.0f)));
            //balls.Add(new Ball(position2, new Vector2(-50.0f, 0.0f)));
        }

        private void Update(object? sender, EventArgs e)
        {
            float dt = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            ui.SetFPSText(dt);

            Vector2 mousePosition = Vector2.Convert(PointToClient(MousePosition));

            UpdateBalls(dt, mousePosition);

            // When the mouse button is held down to spawn a block, the preview block
            // has its proportions changed so it can act as a preview for the new block being created
            if (holdingMouse && nextSpawn == SpawnNext.Block)
                previewBlock.SetRectFromMousePosition(lastClickPos, mousePosition);

            lastMousePos = mousePosition;

            Invalidate();
        }
        private void UpdateBalls(float dt, Vector2 mousePosition)
        {
            // Run a timer to automatically spawn balls in if active
            if (autoSpawnBalls && nextSpawn == SpawnNext.Ball)
            {
                ballSpawnTime += dt;
                while (ballSpawnTime >= GameConfig.BallAutoSpawnTimer)
                {
                    SpawnBall(mousePosition);
                    ballSpawnTime -= GameConfig.BallAutoSpawnTimer;
                }
            }

            // Set the velocity text
            if (GameConfig.UseMouseVelocity) ui.SetVelocityText((mousePosition - lastMousePos));

            // Instead of running physics every frame, I run it at a fix 50 fps (0.02 seconds)
            physicsTickTime += dt;
            while (physicsTickTime >= GameConfig.PhysicsFixedTickTimer)
            {
                // I split up the physics into separate parts (substeps)
                // This, along with having a fixed Physics run time, allows for more accurate collisions
                for (int i = 0; i < GameConfig.PhysicsSubsteps; i++)
                    HandlePhysics(FixedDeltaTime);
                physicsTickTime -= GameConfig.PhysicsFixedTickTimer;
            }
        }
        private void HandlePhysics(float dt) {
            // Process the active balls to make them move and collide
            HashSet<Ball> toRemove = new HashSet<Ball>();

            // Move them first before checking collision, necessary fro accurate collision)
            foreach (Ball ball in balls)
                ball.Move(dt);

            for (int i = 0; i < GameConfig.CollisionIterations; i++)
            {
                foreach (Ball ball in balls)
                {
                    bool hitABlock = false;
                    foreach (Block block in blocks){
                        if (ball.Collides(block)){
                            ball.ResolveCollision(block);
                            hitABlock = true;
                        }
                    }
                    if (!hitABlock) ball.grounded = false;

                    foreach (Ball ball2 in balls){
                        if (ball != ball2 && ball.Collides(ball2))
                            ball.ResolveCollision(ball2);
                    }
                }
            }
            foreach (Ball ball in balls) {
                if (ball.Collides(screenBounds))
                    if (GameConfig.BallBorderBehavior == BorderBehavior.Destroy) toRemove.Add(ball);
                    else ball.ResolveCollision(screenBounds);
            }

            foreach (Ball ball in toRemove)
                balls.Remove(ball);
        }

        // Balls
        public void SpawnBall(Vector2 mousePos) {
            // Called on mouse press and spawns on the mouse
            // Return if there are no Balls left
            if (balls.Count > MAX_BALLS) return;
            Debug.WriteLine("Spawning a ball. ball.Count = " + balls.Count);

            Vector2 velocity;
            if (!GameConfig.UseMouseVelocity) velocity = ballLaunchVelocity;
            else
            {
                velocity = mousePos - lastMousePos;
                Debug.WriteLine("Mouse velocity = " + velocity.ToString());
            }

            balls.Add(new Ball(mousePos, velocity));
        }

        // Blocks
        public void SpawnBlock() {
            if (blocks.Count > MAX_BLOCKS) return;
            Debug.WriteLine("Spawning a block. Block.Count = " + blocks.Count);

            // Size is the absolute distance between click and release
            Vector2 size = new Vector2(
                Math.Abs(lastClickPos.x - lastMousePos.x),
                Math.Abs(lastClickPos.y - lastMousePos.y)
            );
        
            // Position is the midpoint between the two points
            Vector2 position = (lastClickPos + lastMousePos) * 0.5f;

            blocks.Add(new Block(position, size));
        }

        private void OnMousePress(object? sender, MouseEventArgs e)
        {
            Debug.WriteLine("Mouse Pressed");

            lastClickPos = Vector2.Convert(PointToClient(MousePosition));
            holdingMouse = true;

            if (nextSpawn == SpawnNext.Ball)
                SpawnBall(lastClickPos);        
        }
        private void OnMouseRelease(object? sender, MouseEventArgs e)
        {
            Debug.WriteLine("Mouse Released");

            holdingMouse = false;

            if (nextSpawn == SpawnNext.Block) SpawnBlock(); 
        }
        private void OnKeyPress(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Q: 
                    if (nextSpawn == SpawnNext.Block) nextSpawn = SpawnNext.Ball;
                    else nextSpawn = SpawnNext.Block;
                    ui.ChangeSpawnText(nextSpawn);
                    break;
                case Keys.W:
                    if (nextSpawn == SpawnNext.Block) blocks.Clear();
                    else balls.Clear();
                    break;
                case Keys.B:
                    GameConfig.BallBorderBehavior = (BorderBehavior)(((int)GameConfig.BallBorderBehavior + 1) % Enum.GetValues<BorderBehavior>().Length);
                    ui.ChangeBorderBehaviorText(GameConfig.BallBorderBehavior);
                    break;
                case Keys.E: autoSpawnBalls = !autoSpawnBalls; break;
                case Keys.Space: GameConfig.UseMouseVelocity = !GameConfig.UseMouseVelocity; break;
                case Keys.Up: ballLaunchVelocity.y -= 10; ui.SetVelocityText(ballLaunchVelocity); break;
                case Keys.Down: ballLaunchVelocity.y += 10; ui.SetVelocityText(ballLaunchVelocity); break;
                case Keys.Left: ballLaunchVelocity.x -= 10; ui.SetVelocityText(ballLaunchVelocity); break;
                case Keys.Right: ballLaunchVelocity.x += 10; ui.SetVelocityText(ballLaunchVelocity); break;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            foreach (Ball ball in balls)
            {
                e.Graphics.FillEllipse(new SolidBrush(ball.color), ball.position.x - ball.radius, ball.position.y - ball.radius,
                    ball.radius * 2.0f, ball.radius * 2.0f);
            }
            foreach (Block block in blocks)
            {
                e.Graphics.FillRectangle(new SolidBrush(block.color), block.rect);
            }
            if (holdingMouse && nextSpawn == SpawnNext.Block)
                e.Graphics.FillRectangle(new SolidBrush(previewBlock.color), previewBlock.rect);

            e.Graphics.DrawString(ui.nextSpawnObjectName, ui.arial, Brushes.White, ui.textPositions[0]);
            e.Graphics.DrawString(ui.destroyObjectsText, ui.arial, Brushes.White, ui.textPositions[1]);
            e.Graphics.DrawString(ui.borderBehaviorText, ui.arial, Brushes.White, ui.textPositions[2]);
            e.Graphics.DrawString(ui.velocityText, ui.arial, Brushes.White, ui.textPositions[3]);
            e.Graphics.DrawString(ui.fpsText, ui.arial, Brushes.White, ui.textPositions[4]);
        }
    }

    // The type of object to be spawned next by the mouse
    public enum SpawnNext
    {
        Block = 0,
        Ball = 1
    }
}
