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
        private float bulletTimeAlive;

        private const float BULLET_ACCELERATION = 3000.0f;
        private const float MAX_BULLET_SPEED = 300.0f;
        private const float MAX_TIME_ALIVE = 0.5f;
        private const float MAX_DISTANCE = 100.0f;

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

        private void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float movement = (Flip == SpriteEffects.None) ? 1.0f : -1.0f;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            velocity.X += movement * MAX_BULLET_SPEED;
         
            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MAX_BULLET_SPEED, MAX_BULLET_SPEED);

            // Apply velocity.
            Position += velocity * elapsed;

            if (Position.X > MAX_DISTANCE || Position.X < -MAX_DISTANCE)
            {
                IsAlive = false;
            }
            else
            {
                Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

                // If the player is now colliding with the level, separate them.
                //HandleCollisions();

                // If the collision stopped us from moving, reset the velocity to zero.
                if (Position.X == previousPosition.X)
                    velocity.X = 0;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive) return;

            ApplyPhysics(gameTime);

            bulletSprite.PlayAnimation(bulletGraphic);
           
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Draw that sprite.
            bulletSprite.Draw(gameTime, spriteBatch, Position, Flip);
        }
    }
}
