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
    class Shotgun : Gun
    {
        private Animation baseGraphic;
        private AnimationPlayer sprite;

        private Animation muzzleFire;
        private AnimationPlayer muzzle;
        private float muzzleAnimationTimer;
        private bool isShooting;

        public Shotgun(Level level, Vector2 position)
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
            baseGraphic = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/shotgun"), 0.1f, false);
            muzzleFire = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/shotgun_muzzle"), 0.01f, false);
        }

        public override void Shoot(Vector2 velocity)
        {
            isShooting = true;
        }

        /// <summary>
        /// Update the gun position and animated muzzle fire
        /// </summary>
        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(45, 0) : new Vector2(-45, 0);
            Flip = flip;
            Position = position + offset;

            sprite.PlayAnimation(baseGraphic);

            if (isShooting)
            {
                muzzleAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                muzzle.PlayAnimation(muzzleFire);
                if (muzzleAnimationTimer > 0.20f)
                {
                    muzzleAnimationTimer = 0.0f;
                    isShooting = false;
                    muzzle.StopAnimation();
                }
            }
        }

        /// <summary>
        /// Draws the gun
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, Flip);

            if (isShooting)
            {
                Vector2 offset = (Flip == SpriteEffects.None) ? new Vector2(32, 0) : new Vector2(-32, 0);
                muzzle.Draw(gameTime, spriteBatch, Position + offset, Flip);
            }
        }

    }
}
