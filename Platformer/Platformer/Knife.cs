using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Platformer
{
    class Knife : Weapon
    {
        Animation idleAnimation;
        Animation attackAnimation;
        AnimationPlayer sprite;

        SoundEffect knifeSound;

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

        public bool IsAttacking { get { return attacking; } }
        private bool attacking = false;
        private float attackTime = 0.0f;
        private const float MAXATTACKTIME = 0.3f;

        private bool canAttack = true;
        private float rateOfAttackTime = 0.0f;
        private const float MAXRATEOFATTACK = 3.0f;

        private const float KNIFE_DAMAGE = 30.0f;

        /// <summary>
        /// Constructs a new Spike
        /// </summary>
        public Knife(PlatformerGame level, Vector2 position, Player player)
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
            idleAnimation = new Animation(game.Content.Load<Texture2D>("Sprites/Weapons/knife_idle"), 0.1f, false, 50);
            attackAnimation = new Animation(game.Content.Load<Texture2D>("Sprites/Weapons/knife"), 0.1f, false, 50);
            sprite.PlayAnimation(idleAnimation);

            knifeSound = game.Content.Load<SoundEffect>("Sounds/knifeSound");
         
            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = 10;
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public override void Shoot()
        {
            if (canAttack)
            {
                attacking = true;
                canAttack = false;
                knifeSound.Play();
                sprite.PlayAnimation(attackAnimation);
            }
        }

        public override void Reset()
        {
            attacking = false;
        }

        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(30, 0) : new Vector2(-30, 0);
            Flip = flip;
            this.position = position + offset;

            if (attacking)
            {
                attackTime += elapsed;
                if (attackTime > MAXATTACKTIME)
                {
                    attacking = false;
                    attackTime = 0.0f;
                    sprite.PlayAnimation(idleAnimation);
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
                                player.Hit(KNIFE_DAMAGE, dir, _player);
                            }
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
                            enemy.Hit(KNIFE_DAMAGE, dir);
                            this.Reset();
                        }
                    }
                }

            }

            if (!canAttack)
            {
                rateOfAttackTime += elapsed;
                if (rateOfAttackTime > MAXRATEOFATTACK)
                {
                    rateOfAttackTime = 0.0f;
                    canAttack = true;
                }
            }
        }

        /// <summary>
        /// Draws the knife
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (canAttack || attacking)
                sprite.Draw(gameTime, spriteBatch, Position, Flip);
        }
    }
}
