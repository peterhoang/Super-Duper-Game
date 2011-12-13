using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Platformer
{
    class HandgunBullet : Bullet
    {
        // graphic of the bullet
        private Animation bulletGraphic;
        private AnimationPlayer bulletSprite;
        private float bulletDistance;

        private const float BULLET_SPEED = 5.0f;
        private const float MAX_BULLET_SPEED = 300.0f;
        private const float MAX_DISTANCE = 150.0f;

        public HandgunBullet(Level level, Vector2 position)
        {
            Level = level;
            Position = position;
            IsAlive = false;
            LoadContent();
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            bulletGraphic = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/bullet"), 0.1f, false);
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive) return;

            bulletSprite.PlayAnimation(bulletGraphic);

            float movement = (Flip == SpriteEffects.None) ? 1.0f : -1.0f;

            float dist = BULLET_SPEED;
            position.X += movement * dist;
            
            bulletDistance += dist;

            if (bulletDistance > MAX_DISTANCE)
            {
                IsAlive = false;
                bulletDistance = 0.0f;
            }
           
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Draw that sprite.
            bulletSprite.Draw(gameTime, spriteBatch, Position, Flip);
        }
    }
}
