using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Physics_Simulation__Create_Task_
{
    // Settings of the game, which includes stats for objects.
    public static class GameConfig
    {
        public static Vector2 Gravity = new Vector2(0.0f, 40.0f);
        public static float AirDrag = 0.01f;

        public static bool UseMouseVelocity = false;
        public static float BallAutoSpawnTimer { get; set => field = Math.Max(value, 0.05f); } = 0.15f;
        public static BorderBehavior BallBorderBehavior = BorderBehavior.Destroy;

        public static float PhysicsFixedTickTimer = 0.02f;
        public static int PhysicsSubsteps = 8;
        public static float FixedDeltaTime = PhysicsFixedTickTimer / PhysicsSubsteps;

        public static int CollisionIterations = 10;

        public static int MaxBalls { get; private set; } = 100;
        public static int MaxBlocks { get; private set; } = 15;
    }
    public static class Utility
    {
        public static Color RandomizeColor()
        {
            const byte minHue = 200;
            return Color.FromArgb(
                255,
                (byte)Random.Shared.Next(minHue, 256),
                (byte)Random.Shared.Next(minHue, 256),
                (byte)Random.Shared.Next(minHue, 256)
            );
        }
        public static float RandomLerp(float[] arr) => arr[0] + (arr[1] - arr[0]) * (float)Random.Shared.NextDouble();
        
    }

    // How Balls act when hitting the sides of teh screen
    public enum BorderBehavior
    {
        Destroy, // Ball is removed from the list
        Wrap, // Ball wraps around the screen
        Bounce // Ball bounces off the screen as if its a block.
    }
}
