using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Platformer
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    abstract class Gun
    {
        public PlatformerGame Level
        {
            get { return level; }
        }
        protected PlatformerGame level;

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

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

        public Gun() { }

        public Gun(Game game)
        {
        }

        protected bool isShooting;

        public abstract void Shoot();

        public abstract void Reset();

        public abstract void Update(GameTime gameTime, Vector2 position, SpriteEffects flip);

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch, bool isRolling);

    }
}
