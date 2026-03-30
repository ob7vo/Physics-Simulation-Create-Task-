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

        // I was going to use an Object Pool (array of stored instances), but since the objects are so small, its redundent
        public List<Ball> balls = new List<Ball>();     
        private List<Block> blocks = new List<Block>();
        private Ball previewBall = new Ball(); // Used for drawing a ball preview before spawning one, AND checking for valid spawns
        private Block previewBlock = new Block();
        
        private Vector2 ballLaunchVelocity = new Vector2(0,0); // Velocity balls get when
        bool holdingMouse = false;
        private Vector2 lastMousePos = new Vector2(0,0); // The mouse position last frame. Used fro getting mouse velocity
        private Vector2 lastClickPos = new Vector2(0,0); // The mouse position when it last clicked. Used for sizing blocks;

        private SpawnNext nextSpawn = SpawnNext.Block;
        float ballSpawnTime = 0.0f;
        float physicsTickTime = 0.0f;
        bool autoSpawnBalls = false;
        
        public Form1()
        {
            InitializeComponent();

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
        }

        private void TestStart()
        {
            // Was used during debugging to quickly create test scenarios

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
            balls.Add(new Ball(position1, new Vector2(50.0f,0.0f), 2));
            //balls.Add(new Ball(position2, new Vector2(-50.0f, 0.0f)));
        }

        private void Update(object? sender, EventArgs e)
        {
            float dt = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();

            ui.SetFPSText(dt);

            Vector2 mousePosition = Vector2.Convert(PointToClient(MousePosition));

            UpdateBalls(dt, mousePosition);

            // Shows a preview (ghost/silhouette) of the block that is attempting to be creating
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
                    HandlePhysics(GameConfig.FixedDeltaTime);
                physicsTickTime -= GameConfig.PhysicsFixedTickTimer;
            }

            // The simulation still lags with many balls onscreen, though this
            // COULD be fixed by integrating a grid system to lessen the collision checks.
        }
        private void HandlePhysics(float dt) {
            // Process the active balls to make them move and collide

            // Shuffle the list to avoid clipping errors caused by strict call orders
            for (int i = balls.Count - 1; i > 0; i--){
                int j = Random.Shared.Next(i + 1);
                (balls[i], balls[j]) = (balls[j], balls[i]);
            }

            // Used for removing balls that collide with the border (With "Destroy" Behavior)
            HashSet<Ball> toRemove = new HashSet<Ball>();

            // Do collision first with verlet integration
            for (int i = 0; i < GameConfig.CollisionIterations; i++)
            {
                foreach (Ball ball in balls)
                {
                    bool hitABlock = false;
                    foreach (Block block in blocks){
                        if (ball.SolveCollision(block))
                            hitABlock = true;
                    }
                    if (!hitABlock) ball.grounded = false;

                    foreach (Ball ball2 in balls){
                        if (ball != ball2)
                            ball.SolveCollision(ball2);
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

            foreach (Ball ball in balls)
                ball.Move(dt);
        }

        // Balls
        public void SpawnBall(Vector2 mousePos) {
            // Called on mouse press and spawns on the mouse's position
          
            // The ball has a random radius, but I set the radius here rather than
            // In the constructor so I can check if there is space to spawn it
            float radius = Utility.RandomLerp(Ball.MIN_MAX_RADIUS);
            if (!CanSpawnBall(mousePos, radius)){
                Debug.WriteLine("Can't spawn a Ball within another object");
                return;
            }

            Debug.WriteLine("Spawning a ball. ball.Count = " + balls.Count);

            Vector2 velocity;
            if (!GameConfig.UseMouseVelocity) velocity = ballLaunchVelocity;
            else
            {
                velocity = mousePos - lastMousePos;
                Debug.WriteLine("Mouse velocity = " + velocity.ToString());
            }

            balls.Add(new Ball(mousePos, velocity, radius));
        }
        bool CanSpawnBall(Vector2 position, float radius)
        {
            if (balls.Count > GameConfig.MaxBalls) return false;

            foreach (Ball ball in balls)
            {
                float dist = Vector2.Distance(position, ball.position);
                if (dist < radius + ball.radius)
                    return false;
            }
            foreach (Block block in blocks)
            {
                Vector2 closestSide = new Vector2(
                    Math.Clamp(position.x, block.Left, block.Right),
                    Math.Clamp(position.y, block.Top, block.Bottom)
                );
                Vector2 dist = position - closestSide;

                if (dist.SqrMagnitude <= (radius * radius))
                    return false;
            }

            return true;
        }

        // Blocks
        public void SpawnBlock() {
            if (blocks.Count > GameConfig.MaxBlocks) return;
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

            ui.SetInputText(true);
        }
        private void OnMouseRelease(object? sender, MouseEventArgs e)
        {
            Debug.WriteLine("Mouse Released");

            holdingMouse = false;

            if (nextSpawn == SpawnNext.Block) SpawnBlock();

            ui.SetInputText(false);
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
                case Keys.E: autoSpawnBalls = !autoSpawnBalls; ui.SetAutoSpawnText(autoSpawnBalls); break;
                case Keys.A: GameConfig.BallAutoSpawnTimer += 0.05f; ui.SetAutoSpawnText(); break;
                case Keys.D: GameConfig.BallAutoSpawnTimer -= 0.05f; ui.SetAutoSpawnText(); break;
                case Keys.Space: 
                    GameConfig.UseMouseVelocity = !GameConfig.UseMouseVelocity;
                    // Set it to ball Launch velocity regardless, since it'll correct itself
                    ui.SetVelocityText(ballLaunchVelocity);
                    break;
                case Keys.Up: ballLaunchVelocity.y -= 5; ui.SetVelocityText(ballLaunchVelocity); break;
                case Keys.Down: ballLaunchVelocity.y += 5; ui.SetVelocityText(ballLaunchVelocity); break;
                case Keys.Left: ballLaunchVelocity.x -= 5; ui.SetVelocityText(ballLaunchVelocity); break;
                case Keys.Right: ballLaunchVelocity.x += 5; ui.SetVelocityText(ballLaunchVelocity); break;
                default: return;
            }

            ui.SetInputText(e.KeyCode);
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
            e.Graphics.DrawString(ui.autoSpawnText, ui.arial, Brushes.White, ui.textPositions[4]);
            e.Graphics.DrawString(ui.fpsText, ui.arial, Brushes.Yellow, ui.textPositions[5]);
            e.Graphics.DrawString(ui.inputText, ui.bigArial, Brushes.Cyan, ui.inputTextPosition);
        }
    }

    // The type of object to be spawned next by the mouse
    public enum SpawnNext
    {
        Block = 0,
        Ball = 1
    }
}
