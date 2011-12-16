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
    class ShotgunShell : Bullet
    {
        private float bulletDistance;

        private const float BULLET_SPEED = 10.0f;
        private const float MAX_BULLET_SPEED = 300.0f;
        private const float MAX_DISTANCE = 40.0f;
        private const float BULLET_DAMAGE = 50.0f;

        public ShotgunShell(Level level, Vector2 position)
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
            bulletGraphic = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/shell"), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int)(bulletGraphic.FrameWidth * 0.4);
            int left = (bulletGraphic.FrameWidth - width) / 2;
            int height = (int)(bulletGraphic.FrameWidth * 0.8);
            int top = bulletGraphic.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
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
            else
            {
                HandleCollisions();
            }
        }

        public override void Reset()
        {
            bulletDistance = 0.0f;
            IsAlive = false;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Draw that sprite.
            bulletSprite.Draw(gameTime, spriteBatch, Position, Flip);
        }


        /// <summary>
        /// Handles the collisions of bullet against another player.
        /// </summary>
        private void HandleCollisions()
        {
            foreach (Player player in Level.Players)
            {
                //do not check against the owner of the bullet
                if (player != _player)
                {
                    if (this.BoundingRectangle.Intersects(player.BoundingRectangle))
                    {
                        //Rolling players are invulnarable. 
                        if (!player.IsRolling && player.IsAlive)
                        {
                            float dir = (Flip == SpriteEffects.None) ? 1.0f : -1.0f;
                            player.Hit(BULLET_DAMAGE, dir, _player);
                            this.Reset();
                        }
                    }
                }
            }
        }

    }
}
