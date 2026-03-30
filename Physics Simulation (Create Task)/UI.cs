using System;
using System.Collections.Generic;
using System.Text;

namespace Physics_Simulation__Create_Task_
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class UI
    {
        public Font arial = new Font("Arial", 10f, FontStyle.Bold);
        public Font bigArial = new Font("Arial", 20f, FontStyle.Bold);

        public string nextSpawnObjectName = "Selected Object (Q to switch, Click to spawn): Block";
        public string destroyObjectsText = "Destroy all Blocks (Press W)";
        public string borderBehaviorText = "Balls' behavior when hitting the screen border (B): Destroy";
        public string velocityText = "";
        public string autoSpawnText = "Auto Ball Spawn Timer: OFF (Press E to turn on/off, press A/D to adjust timer)";
        public string fpsText = "";

        public string inputText = "Last Input: NONE";

        public PointF[] textPositions = new PointF[6];
        public PointF inputTextPosition = new PointF();
        public UI(Rectangle screenBorder)
        {
            for (int i = 0; i < textPositions.Length; i++)
            {
                textPositions[i].X = screenBorder.X + screenBorder.Width * 0.04f;
                textPositions[i].Y = screenBorder.Y + screenBorder.Height * (0.02f * (i + 1));
            }

            inputTextPosition.X = screenBorder.X + (screenBorder.Width * 0.7f);
            inputTextPosition.Y = screenBorder.Y + (screenBorder.Height * 0.075f);
        }

        public void ChangeSpawnText(SpawnNext next)
        {
            nextSpawnObjectName = "Selected Object (Q to switch, Click to spawn): " + next.ToString();
            destroyObjectsText = "Destroy all " + next.ToString() + "s with (W)";
        }
        public void ChangeBorderBehaviorText(BorderBehavior behavior)
        {
            borderBehaviorText = "Balls' behavior when hitting the screen border (B): " + behavior.ToString();
        }
        public void SetVelocityText(Vector2 newVel)
        {
            velocityText = "Ball Launch Velocity (Arrow Keys or Space): " + newVel.ToString();
        }
        public void SetVelocityText(Vector2 lastMousePos, Vector2 newMousePos)
        {
            velocityText = "Mouse Velocity (Space): " + (newMousePos - lastMousePos).ToString();
        }
        public void SetFPSText(float dt)
        {
            fpsText = $"FPS: {1 / dt:F0}";
        }
        public void SetInputText(Keys key)
        {
            inputText = "Last Input: " + key.ToString();
        }
        public void SetInputText(bool mouseDown)
        {
            inputText = "Last Input: " + (mouseDown ? "Mouse Press" : "Mouse Release");
        }
        public void SetAutoSpawnText(bool autoSpawn)
        {
            string onOff = autoSpawn ? "ON" : "OFF";
            autoSpawnText = $"Auto Ball Spawn Timer: {onOff} (Press E to turn on/off, press A/D to adjust timer)";
        }
        public void SetAutoSpawnText()
        {
            autoSpawnText = $"Auto Ball Spawn Timer: {GameConfig.BallAutoSpawnTimer} (Press E to turn on/off, press A/D to adjust timer)";
        }
    }
}
