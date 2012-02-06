using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    public class Spike
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Position in world space of the bottom center of this enemy.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this enemy in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        private Animation idleAnimation;
        private AnimationPlayer sprite;

        /// <summary>
        /// Constructs a new Spike
        /// </summary>
        public Spike(Level level, Vector2 position)
        {
            this.level = level;
            this.position = position;

            LoadContent();
        }

        /// <summary>
        /// Loads a particular spike sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Tiles/spike"), 0.1f, false);
            sprite.PlayAnimation(idleAnimation);
         
            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.35);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Draws the spike
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            sprite.Draw(gameTime, spriteBatch, Position, SpriteEffects.None);
        }
    }
}
