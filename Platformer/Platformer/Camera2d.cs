using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Platformer
{
    public class Camera2d
    {
        private Level level;

        public Camera2d(Level level)
        {
            this.level = level;
        }

        private float cameraPosition;
        public float CameraPosition
        {
            get { return cameraPosition; }
        }

        public void ScrollCamera(Player follow, Viewport viewport)
        {
            const float ViewMargin = 0.35f;

            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition + marginWidth;
            float marginRight = cameraPosition + viewport.Width - marginWidth;

            // Calculate how far to scroll when the player is near the edges of the screen.
            float cameraMovement = 0.0f;
            if (follow.Position.X < marginLeft)
                cameraMovement = follow.Position.X - marginLeft;
            else if (follow.Position.X > marginRight)
                cameraMovement = follow.Position.X - marginRight;

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPosition = Tile.Width * level.Width - viewport.Width;
            cameraPosition = MathHelper.Clamp(cameraPosition + cameraMovement, 0.0f, maxCameraPosition);
        }
    }
}
