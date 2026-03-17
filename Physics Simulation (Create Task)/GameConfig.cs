using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Physics_Simulation__Create_Task_
{
    // Settings of the game, which includes stats for objects.
    public static class GameConfig
    {
        // Settings for Blocks
     

        // Settings for balls
        public static float Gravity = 40.0f;
        public static float AirDrag = 0.01f;
        public static float ImpactLoss = 0.9f; // Multiplier for velocity lost on ball-to-block collision
        public static bool UseMouseVelocity = false;
       
        public static int MaxBalls { get; private set; } = 100;
        public static int MaxBlocks { get; private set; } = 15;
    }
    public static class Utility
    {
        public static Color RandomizeColor()
        {
            return Color.FromArgb(
                255,
                (byte)Random.Shared.Next(0, 256),
                (byte)Random.Shared.Next(0, 256),
                (byte)Random.Shared.Next(0, 256)
            );
        }
        public static float RandomLerp(float[] arr) => arr[0] + (arr[1] - arr[0]) * (float)Random.Shared.NextDouble();
        
    }
}
