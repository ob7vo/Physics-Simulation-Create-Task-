using System.Diagnostics;
using System.Timers;

namespace Physics_Simulation__Create_Task_
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer;
        private Stopwatch stopwatch = new Stopwatch();
        Rectangle screenBounds;

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
        public Form1()
        {
            InitializeComponent();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 16; // ~60fps (milliseconds)
            timer.Tick += Update;
            timer.Start();

            stopwatch.Start();

            MouseDown += OnMousePress;
            MouseUp += OnMouseRelease;

            screenBounds = this.ClientRectangle;
            this.DoubleBuffered = true;  // Buffers drawing off-screen, then blits all at once
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |   // Skip WM_ERASEBKGND message
                ControlStyles.UserPaint |              // We handle painting
                ControlStyles.OptimizedDoubleBuffer,   // Off-screen buffer
                true
            );
            this.UpdateStyles();
            this.BackColor = Color.Navy;

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
                screenBounds.X + screenBounds.Width * 0.3f,
                screenBounds.Y + screenBounds.Height * 0.5f
                );
            Vector2 position2 = new Vector2(
                screenBounds.X + screenBounds.Width * 0.7f,
                screenBounds.Y + screenBounds.Height * 0.75f
                );
            // Spawn a ball
            balls.Add(new Ball(position1, new Vector2(50.0f,0.0f)));
            //balls.Add(new Ball(position2, new Vector2(-50.0f, 0.0f)));
        }

        private void Update(object? sender, EventArgs e)
        {
            float dt = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();

            ProcessBalls(dt);

            lastMousePos = Vector2.Convert(MousePosition);

            Invalidate();
        }

        private void ProcessBalls(float dt) {
            // Process the active balls to make them move and collide
            foreach (Ball ball in balls) {
                // Move them first before checking collision, necessary fro accurate collision)
                ball.Move(dt);

                // Check Collision with walls first before balls (arbitrary order I chose)
                bool hitABlock = false;
                foreach (Block block in blocks) {
                    if (ball.Collides(block)){
                        ball.ResolveCollision(block);
                        hitABlock = true;
                    }
                }
                if (!hitABlock) ball.grounded = false;

                // Check collision between other balls last
                foreach (Ball ball2 in balls) {
                    // Check if (i != j) so the ball wont collide with itself
                    if (ball != ball2 && ball.Collides(ball2))
                        ball.ResolveCollision(ball2);
                }
                
            }
        }

        // Balls
        public void SpawnBall(Vector2 mousePos) {
            // Called on mouse press and spawns on the mouse
            // Return if there are no Balls left
            if (!IsAvailableBall() || !IsValidBallSpawnPosition()) return;
            
            Vector2 velocity;
            if (!GameConfig.UseMouseVelocity) velocity = ballLaunchVelocity;
            else velocity = mousePos - lastMousePos;

            balls.Add(new Ball(mousePos, velocity));
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
            if (!IsAvailableBlock()) return;
            
            // Size is the absolute distance between click and release
            Vector2 size = new Vector2(
                Math.Abs(lastClickPos.x - mousePos.x),
                Math.Abs(lastClickPos.y - mousePos.y)
            );
        
            // Position is the midpoint between the two points
            Vector2 position = (lastClickPos + mousePos) * 0.5f;

            blocks.Add(new Block(position, size));
        }
        public bool IsAvailableBlock() => blocks.Count < MAX_BLOCKS;

        private void OnMousePress(object? sender, MouseEventArgs e)
        {
            lastClickPos = Vector2.Convert(MousePosition);
        }
        private void OnMouseRelease(object? sender, MouseEventArgs e)
        {

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
        }
    }
}
