using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Platformer
{
    /// <summary>
    /// A valuable item the player can collect.
    /// </summary>
    class Corpse
    {
        private Texture2D texture;
        private Vector2 origin;

        public const int PointValue = 30;
        public readonly Color Color = Color.Yellow;

        // The Corpse is animated from a base position along the Y axis.
        private Vector2 basePosition;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Gets the current position of this Corpse in world space.
        /// </summary>
        public Vector2 Position
        {
            get { return basePosition; }
            set { basePosition = value; }
        }

        public SpriteEffects Flip = SpriteEffects.None;

        public bool IsActive = false;

        /// <summary>
        /// Constructs a new Corpse.
        /// </summary>
        public Corpse(Level level, Vector2 position, string textureFile)
        {
            this.level = level;
            this.basePosition = position;

            LoadContent(textureFile);
        }

        /// <summary>
        /// Loads the Corpse texture and collected sound.
        /// </summary>
        public void LoadContent(string textureFile)
        {
            texture = Level.Content.Load<Texture2D>(textureFile);
            origin = new Vector2(texture.Width / 2.0f, texture.Height);
        }

        /// <summary>
        /// Draws a Corpse in the appropriate color.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, Position, null, Color.White, 0.0f, origin, 1.0f, Flip, 0.0f);
        }
    }
}
