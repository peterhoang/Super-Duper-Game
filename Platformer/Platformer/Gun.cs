using System;
using System.Collections.Generic;
using System.Linq;
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
        private Animation _baseGraphic;

        public Level Level
        {
            get { return level; }
            set { level = value; }
        }
        Level level;

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

        public Gun() { }

        public Gun(Game game)
        {
        }

        public abstract void Shoot();

        public abstract void Update(GameTime gameTime, Vector2 position, SpriteEffects flip);

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    }
}
