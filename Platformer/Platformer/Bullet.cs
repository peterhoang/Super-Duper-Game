using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    abstract class Bullet
    {
        public PlatformerGame Game
        {
            get { return game; }
        }
        protected PlatformerGame game;

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        protected Vector2 position;

        public SpriteEffects Flip
        {
            get { return flip; }
            set { flip = value; }
        }
        SpriteEffects flip;

        public bool IsAlive
        {
            get { return isAlive; }
            set { isAlive = value; }
        }
        bool isAlive;

        public Player Player
        {
            get { return _player; }
            set { _player = value; }
        }
        protected Player _player;


        // graphic of the bullet
        protected Animation bulletGraphic;
        protected AnimationPlayer bulletSprite;
        protected Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this bullet in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - bulletSprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - bulletSprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        public Bullet() { }

        public abstract void Reset();

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    }
}
