using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    abstract class Weapon
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

        public Player Player
        {
            get { return _player; }
            set { _player = value; }
        }
        protected Player _player;

        public Weapon() { }

        public Weapon(Game game)
        {
        }

        protected bool isShooting;

        public abstract void Shoot();

        public abstract void Reset();

        public abstract void Update(GameTime gameTime, Vector2 position, SpriteEffects flip);

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    }
}
