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
    class BowserFire : Bullet
    {
        private const float BULLET_SPEED = 100.0f;
        private const float BULLET_DAMAGE = 35f;

        private const float MAX_BULLET_SPEED = 100.0f;
        private const float MAX_BULLET_TIMEALIVE = 4.0f;

        public FaceDirection direction = FaceDirection.Left;

        private float bulletTimeAlive; 

        public BowserFire(PlatformerGame level, Vector2 position)
        {
            this.level = level;
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
            bulletGraphic = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/firebreath"), 0.1f, true, 60);
            bulletSprite.PlayAnimation(bulletGraphic);

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

            float elasped = (float)gameTime.ElapsedGameTime.TotalSeconds;
           
            position.X += (int)direction * BULLET_SPEED * elasped;
            bulletTimeAlive += elasped;

            if (bulletTimeAlive > MAX_BULLET_TIMEALIVE)
            {
                IsAlive = false;
                bulletTimeAlive = 0.0f;
            }
            else
            {
                HandleCollisions();
            }
        }

        public override void Reset()
        {
            bulletTimeAlive = 0.0f;
            IsAlive = false;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Draw that sprite.
            Flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            bulletSprite.Draw(gameTime, spriteBatch, Position, Flip);
        }

        /// <summary>
        /// Handles the collisions of bullet against another player.
        /// </summary>
        private void HandleCollisions()
        {
            foreach (Player player in PlatformerGame.Players)
            {
                //do not check against the owner of the bullet
                if (player != _player)
                {
                    if (this.BoundingRectangle.Intersects(player.BoundingRectangle))
                    {
                        //Rolling players are invulnarable. 
                        //Ignore dead players
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
