using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    class GrenadeExplosion : Bullet
    {
        private const float DAMAGE = 100.0f;

        public GrenadeExplosion(PlatformerGame game, Vector2 position, Player player)
        {
            this.game = game;
            this._player = player;
            Position = position;
            IsAlive = false;
            LoadContent();
        }

        public void LoadContent()
        {
            // Load animated textures.
            bulletGraphic = new Animation(Game.Content.Load<Texture2D>("Sprites/Weapons/explosion"), 0.1f, false);
            bulletSprite.PlayAnimation(bulletGraphic);

            bulletSprite.AnimationCompleted += new AnimationCompletedHanler(bulletSprite_AnimationCompleted);

            // Calculate bounds within texture size.            
            int width = (int)(bulletGraphic.FrameWidth);
            int left = (bulletGraphic.FrameWidth - width) / 2;
            int height = (int)(bulletGraphic.FrameWidth);
            int top = bulletGraphic.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        void bulletSprite_AnimationCompleted(object sender, EventArgs e)
        {
            bulletSprite.StopAnimation();
            IsAlive = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive) return;

            bulletSprite.PlayAnimation(bulletGraphic);

            foreach (Player player in PlatformerGame.Players)
            {
                if (this._player != player)
                {
                    if (player.IsAlive && this.BoundingRectangle.Intersects(player.BoundingRectangle))
                    {
                        float dir = (Flip == SpriteEffects.None) ? 1.0f : -1.0f;
                        player.Hit(DAMAGE, dir, _player);
                    
                    }
                }
            }

            foreach (Bowser enemy in game.CurrentLevel.Enemies)
            {
                if (enemy.IsAlive)
                {
                    if (this.BoundingRectangle.Intersects(enemy.BoundingRectangle))
                    {
                        int dir = (Flip == SpriteEffects.None) ? 1 : -1;
                        enemy.Hit(DAMAGE, dir);
                    }
                }
            }

        }

        public override void Reset()
        {
            IsAlive = false;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Draw that sprite.
            bulletSprite.Draw(gameTime, spriteBatch, Position, Flip);
        }

    }
}
