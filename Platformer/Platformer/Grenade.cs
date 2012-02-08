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

        SoundEffect throwSound;
        SoundEffect bombSound;
        SoundEffect explosionSound;
        SoundEffectInstance bombSoundInstance;

        private int previousBottom;

        public bool IsAttacking { get { return attacking; } }
        private bool attacking = false;
        private bool charging = false;
        private bool initialVelocitySet = false;
      
        private float timeToLive = 0.0f;
        private const float MAX_TIME_ALIVE = 1.5f;

        public bool CanAttack { get { return canAttack; } }
        private bool canAttack = true;
        private float rateOfAttackTime = 0.0f;
        private const float MAXRATEOFATTACK = 1.5f;

        private Vector2 velocity;

        // Constants for controlling vertical movement
        private float jumpTime;
        private float MaxJumpTime = 1.0f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 1000.0f;
        private const float MaxFallSpeed = 550.0f;
        private const float JumpControlPower = 0.14f;

        public bool IsOnGround { get; set; }
        public bool IsAlive { get; set; }

        GrenadeExplosion explosion;

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
            this.Reset();

            explosion = new GrenadeExplosion(game, position, _player);

            idleAnimation = new Animation(game.Content.Load<Texture2D>("Sprites/Weapons/bomb"), 0.1f, true);
            sprite.PlayAnimation(idleAnimation);

            explosionSound = game.Content.Load<SoundEffect>("Sounds/explosionSound");

            throwSound = game.Content.Load<SoundEffect>("Sounds/throwSound");
            bombSound = game.Content.Load<SoundEffect>("Sounds/bombSound");
            bombSoundInstance = bombSound.CreateInstance();
            bombSoundInstance.IsLooped = true;
            bombSoundInstance.Volume = 0.55f;
     
         
            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = 10;
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public void Charging(Vector2 position)
        {
            if (attacking || !canAttack) return;

            if (bombSoundInstance.State != SoundState.Playing)
            {
                bombSoundInstance.Play();
            }
            sprite.PlayAnimation(idleAnimation);
            charging = true;

            if (!initialVelocitySet)
            {
                velocity = (_player.Flip == SpriteEffects.None) ? new Vector2(200.0f, -100.0f) : new Vector2(-200.0f, -100.0f);
                initialVelocitySet = true;
            }
        }

        public override void Shoot()
        {
            if (!canAttack) return;
            canAttack = false;
            charging = false;
            attacking = true;
            throwSound.Play();
            bombSoundInstance.Stop();
        }

        public override void Reset()
        {
            IsAlive = true;
            initialVelocitySet = false;
            attacking = false;
            charging = false;
            timeToLive = 0.0f;
            jumpTime = 0.0f;
            velocity = new Vector2(200.0f, -100.0f);
        }

        private void Explode()
        {
            explosionSound.Play();
            explosion.Position = this.position;
            explosion.IsAlive = true;
            this.Reset();    
        }

        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
          
            if (charging && !attacking)
            {
                velocity.X += 5.0f * ((flip == SpriteEffects.None) ? 1.0f : -1.0f);
                velocity.Y -= 50.0f;
                MaxJumpTime += 1.0f;
            }

            if (attacking)
            {
                velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);
                velocity.Y = DoJump(velocity.Y, gameTime);
                this.position.X += velocity.X * elapsed;
                this.position.Y += velocity.Y * elapsed;
                //this.position = new Vector2((float)Math.Round(this.position.X), (float)Math.Round(this.position.Y));

                HandleCollisions();

                timeToLive += elapsed;
                if (timeToLive > MAX_TIME_ALIVE)
                {
                    this.Reset();
                }
            }
            else
            {
                Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(-20, -18) : new Vector2(20, -18);
                Flip = flip;
                this.position = position + offset;
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

            explosion.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (attacking || charging) 
                sprite.Draw(gameTime, spriteBatch, this.position, Flip);

            explosion.Draw(gameTime, spriteBatch);
                //basicEffect.CurrentTechnique.Passes[0].Apply();
                //game.ScreenManager.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, vertices, 0, 1);
            
        }

        private void HandleCollisions()
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            IsOnGround = false;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = game.CurrentLevel.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = game.CurrentLevel.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform || collision == TileCollision.Impassable || IsOnGround)
                            {
                                this.Explode();
                                break;
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }

        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (!charging && attacking)
            {
                // Begin or continue a jump
                if ((IsOnGround) || jumpTime > 0.0f)
                {
                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                }

                // If we are in the ascent of the jump
                if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }

            return velocityY;
        }
    }
}
