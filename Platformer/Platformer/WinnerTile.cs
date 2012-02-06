using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    class WinnerTile
    {
        #region Fields

        public Level Level
        {
            get { return level; }
        }
        Level level;

        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        private Rectangle localBounds;
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        Animation idleAnimation;
        Animation cheerAnimation;
        AnimationPlayer sprite;

        #endregion

        #region The rest...

        public WinnerTile(Level level, Vector2 position)
        {
            this.level = level;
            this.position = position;

            LoadContent();
        }

        public void LoadContent()
        {
            int rand = PlatformerGame.RandomBetween(0, 100);
            if (rand >= 0 && rand <= 25)
            {
                idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/donut"), 0.1f, false);
            }
            else if (rand > 25 && rand <= 50)
            {
                idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/duff"), 0.1f, false, 71);
            }
            else if (rand > 50 && rand <= 75)
            {
                idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/chunli"), 0.1f, false, 40);
            }
            sprite.PlayAnimation(idleAnimation);
         
            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 1.5);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            sprite.Draw(gameTime, spriteBatch, Position, SpriteEffects.None);
        }

        #endregion
    }
}
