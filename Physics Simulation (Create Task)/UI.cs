using System;
using System.Collections.Generic;
using System.Text;

namespace Physics_Simulation__Create_Task_
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class UI
    {
        public Font arial = new Font("Arial", 7.5f, FontStyle.Bold);
        public string nextSpawnObjectName = "Selected Object Type (Press Q to switch): Block";
        public string destroyObjectsText = "Press W to destroy all objects of the selected type.";
        public string borderBehaviorText = "Balls' behavior when hitting the screen border (Press B to switch): Destroy";
        public string velocityText = "";
        public string fpsText = "";

        public PointF[] textPositions = new PointF[5];
        public UI(Rectangle screenBorder)
        {
            for (int i = 0; i < textPositions.Length; i++)
            {
                textPositions[i].X = screenBorder.X + screenBorder.Width * 0.1f;
                textPositions[i].Y = screenBorder.Y + screenBorder.Height * (0.08f * (i + 1));
            }
        }

        public void ChangeSpawnText(SpawnNext next)
        {
            nextSpawnObjectName = "Selected Object Type (Press Q to switch): " + next.ToString();
        }
        public void ChangeBorderBehaviorText(BorderBehavior behavior)
        {
            borderBehaviorText = "Balls' behavior when hitting the screen border (Press B to switch): " + behavior.ToString();
        }
        public void SetVelocityText(Vector2 newVel)
        {
            velocityText = "Ball Launch Velocity (Change with arrow keys, press space switch): " + newVel.ToString();
        }
        public void SetVelocityText(Vector2 lastMousePos, Vector2 newMousePos)
        {
            velocityText = "Ball Launch Velocity (Using the mouse, press space to switch): " + (newMousePos - lastMousePos).ToString();
        }
        public void SetFPSText(float dt)
        {
            fpsText = $"FPS: {1 / dt:F0}";
        }
    }
}
