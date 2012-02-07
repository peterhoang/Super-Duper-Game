using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Platformer
{
    class Grenade : Weapon
    {
        Animation idleAnimation;
        AnimationPlayer sprite;

        SoundEffect grenadeSound;
        SoundEffect explosionSound;

        public bool IsAttacking { get { return attacking; } }
        private bool attacking = false;
        private float attackTime = 0.0f;
        private const float MAXATTACKTIME = 0.3f;

        private bool canAttack = true;
        private float rateOfAttackTime = 0.0f;
        private const float MAXRATEOFATTACK = 1.5f;

        private const float BOMB_DAMAGE = 1.0f;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this enemy in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        public Grenade(PlatformerGame level, Vector2 position, Player player)
        {
            this.game = level;
            this.position = position;
            this._player = player;

            LoadContent();
        }

        /// <summary>
        /// Loads a particular knife sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            idleAnimation = new Animation(game.Content.Load<Texture2D>("Sprites/Weapons/bomb"), 0.1f, true);
            sprite.PlayAnimation(idleAnimation);

            grenadeSound = game.Content.Load<SoundEffect>("Sounds/grenadeSound");
            explosionSound = game.Content.Load<SoundEffect>("Sounds/explosionSound");
         
            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = 10;
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public override void Shoot()
        {
            attacking = true;
        }

        public override void Reset()
        {
            attacking = false;
        }

        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(-20, -18) : new Vector2(20, -18);
            Flip = flip;
            this.position = position + offset;

            if (attacking)
            {

            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            //if (attacking)
                sprite.Draw(gameTime, spriteBatch, Position, Flip);
        }
    }
}
