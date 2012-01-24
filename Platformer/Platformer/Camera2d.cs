using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Platformer
{
    public class Camera2d
    {
        private Level level;

        const float ViewMargin = 0.35f;

        public Camera2d(Level level, float pos)
        {
            this.level = level;
            this.cameraPosition = pos;
        }

        private float cameraPosition;
        public float CameraPosition
        {
            get { return cameraPosition; }
            set { cameraPosition = value; }
        }

        public float GetSpawnPoint(Viewport viewport)
        {
            float results = -1.0f;

            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition + marginWidth;
            float marginRight = cameraPosition + viewport.Width - marginWidth;

            float offset = 10.0f * Tile.Width;
          
            // don't spawn outside the map
            float maxcamerapos = Tile.Width * level.Width - viewport.Width;

            // player 1
            if (PlatformerGame.attacker_id == 0)
            {
                results = marginRight + offset;
                return (results >= Tile.Width * level.Width) ? -1.0f : results;
            }
            // player 2
            else
            {
                results = marginLeft - offset;
                return (results <= 0.0f) ? -1.0f : results;
            }
        }

        public void ScrollCamera(Vector2 follow, Viewport viewport)
        {
            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition + marginWidth;
            float marginRight = cameraPosition + viewport.Width - marginWidth;

            // Calculate how far to scroll when the player is near the edges of the screen.
            float cameraMovement = 0.0f;
            if (follow.X < marginLeft)
                cameraMovement = follow.X - marginLeft;
            else if (follow.X > marginRight)
                cameraMovement = follow.X - marginRight;

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPosition = Tile.Width * level.Width - viewport.Width;
            cameraPosition = MathHelper.Clamp(cameraPosition + cameraMovement, 0.0f, maxCameraPosition);
        }
    }
}
