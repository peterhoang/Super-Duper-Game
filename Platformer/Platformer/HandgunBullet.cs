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
   
        private const float BULLET_SPEED = 10.0f;
        private const float BULLET_DAMAGE = 25.0f;
        
        private const float MAX_BULLET_SPEED = 500.0f;
        private const float MAX_BULLET_TIMEALIVE = 0.3f;
   
        private float bulletTimeAlive; 
        


        public HandgunBullet(PlatformerGame level, Vector2 position)
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
            bulletGraphic = new Animation(Level.Content.Load<Texture2D>("Sprites/Weapons/bullet"), 0.1f, false);

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
            
            bulletTimeAlive += (float)gameTime.ElapsedGameTime.TotalSeconds;

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
            bulletSprite.Draw(gameTime, spriteBatch, Position, Flip);
        }


        /// <summary>
        /// Handles the collisions of bullet against another player.
        /// </summary>
        private void HandleCollisions()
        {
            //check against tile
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.CurrentLevel.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.CurrentLevel.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            if (collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                this.Reset();
                                return;
                            }
                        }
                    }
                }
            }

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
