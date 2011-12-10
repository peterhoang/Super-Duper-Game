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
    class HandGun : Gun
    {
        private Animation baseGraphic;
        private AnimationPlayer sprite;

        private Animation muzzleFire;
        private AnimationPlayer muzzle;

        public bool IsShooting
        {
            get { return isShooting; }
            set { isShooting = value; }
        }
        private bool isShooting;

        public HandGun(Level level, Vector2 position)
        {
            Level = level;
            Position = position;
            LoadContent();
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            baseGraphic = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/handgun"), 0.1f, false);
            muzzleFire = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/handgun_muzzle"), 0.1f, false);
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        public void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(45, 0) : new Vector2(-45, 0);
            Flip = flip;
            Position = position + offset;

            sprite.PlayAnimation(baseGraphic);
        }

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, Flip);
        }
    }
}
