using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;


namespace Platformer
{
    public enum EntityState
    {
        IDLE = 0, RUNNING = 1, ATTACKING = 2, SEARCHING = 3, JUMPING = 4, INAIR = 5
    };

    public class Bowser : Enemy
    {
        #region Fields

        Animation fireAnimation;
        Animation jumpAnimation;

        public EntityState State { get; set; }

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 1.0f;
        private const float JumpLaunchVelocity = -800.0f;
        private const float GravityAcceleration = 150.0f;
        private const float MaxFallSpeed = 350.0f;
        private const float JumpControlPower = 0.14f;
        private const float MaxDistFromPlayer = 150.0f;
        private const float MinDistFromPlayer = 80.0f;
        private const int MAX_FIRE = 1;

        private float jumpTime = 0.0f;
        private const float MaxMoveSpeed = 50.0f;
        protected Vector2 velocity;

        private List<BowserFire> _bullets = new List<BowserFire>();

        Texture2D dummyTexture;

        private float previousBottom;

        public bool IsOnGround { get; set; }
        public bool IsAlive { get; set; }

        //Sounds
        private SoundEffect fireSound;
        private SoundEffect hitSound;
        private SoundEffect bowserFallSound;

        //Sprite Effects: Pulse red
        private bool pulseRed;
        private float pulseRedTime;
        private const float MAX_PULSE_TIME = 0.4f;
        private int gotHitFrom;

        private float health;
        private bool playDeathAnimation;

        Texture2D debugTexture;

        #endregion


        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Bowser(Level level, Vector2 position, string spriteSet)
            : base(level, position, spriteSet)
        {
            State = EntityState.IDLE;
            velocity = Vector2.Zero;
            IsAlive = true;

            for (int i = 0; i < MAX_FIRE; i++)
            {
                _bullets.Add(new BowserFire(level.Game, position));
            }
        }

        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public override void LoadContent(string spriteSet)
        {
            debugTexture = new Texture2D(Level.Game.ScreenManager.GraphicsDevice, 1, 1);
            debugTexture.SetData(new Color[] { Color.White });

            // Load animations.
            base.LoadContent(spriteSet);
            fireAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/" + spriteSet + "/Fire"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/" + spriteSet + "/Jump"), 0.1f, true);

            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.4);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            dummyTexture = new Texture2D(Level.Game.ScreenManager.GraphicsDevice, 1, 1);
            dummyTexture.SetData(new Color[] { Color.White });

            fireSound = level.Content.Load<SoundEffect>("Sounds/smb_bowserfire");
            hitSound = level.Content.Load<SoundEffect>("Sounds/hitSound");
            bowserFallSound = level.Content.Load<SoundEffect>("Sounds/smb_bowserfalls");


            health = 100.0f;
        }

        public void Killed()
        {
            IsAlive = false;
            foreach (BowserFire fire in _bullets)
            {
                fire.Reset();
                fire.IsAlive = false;
            }
            bowserFallSound.Play();
        }

        public override void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (playDeathAnimation)
            {
                velocity.X = 0.0f;
                velocity.Y += 0.4f;

                // Move in the current direction.
                // Apply velocity.
                position += velocity * elapsed;
                position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));
            }
            else if (IsAlive)
            {
                

                if (IsAlive && IsOnGround)
                {
                    if (Math.Abs(velocity.X) - 0.02f > 0)
                    {
                        sprite.PlayAnimation(runAnimation);
                    }
                    else
                    {
                        sprite.PlayAnimation(idleAnimation);
                    }
                }

                //random jumping
                waitTime += elapsed;
                if (waitTime >= MaxWaitTime)
                {
                    int rand = PlatformerGame.RandomBetween(1, 100);
                    if (rand < 75)
                    {
                        this.State = EntityState.RUNNING;
                    }
                    else
                    {
                        this.State = EntityState.JUMPING;
                    }
                    if (rand > 50)
                    {
                        FireAttack();
                        sprite.PlayAnimation(fireAnimation);
                    }
                    waitTime = 0.0f;
                }

                // pursue the attacker
                Player player = PlatformerGame.Players[PlatformerGame.attacker_id];
                float deltaDist = this.position.X - player.Position.X;

                if (deltaDist < 0)
                {
                    direction = FaceDirection.Right;
                }
                else
                {
                    direction = FaceDirection.Left;
                }

                // Maintain a min distance from player
                if (Math.Abs(deltaDist) > PlatformerGame.RandomBetween(MinDistFromPlayer, MaxDistFromPlayer))
                {
                    velocity.X += (int)direction * MoveSpeed * elapsed;
                }
                else
                {
                    velocity.X += -(int)direction * MoveSpeed * elapsed;
                }

                velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);
                velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);
                velocity.Y = DoJump(velocity.Y, gameTime);

                // Move in the current direction.
                // Apply velocity.
                position += velocity * elapsed;
                position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));

                //Update the fire attack
                foreach (BowserFire fire in _bullets)
                {
                    fire.Update(gameTime);
                }

                HandleCollisions();

                //Sprite effects
                if (pulseRed)
                {
                    pulseRedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (pulseRedTime > MAX_PULSE_TIME)
                    {
                        pulseRed = false;
                        pulseRedTime = 0.0f;
                    }
                }
            }
        }

        public void FireAttack()
        {
            //fire off a bullet if any available
            foreach (BowserFire bullet in _bullets)
            {
                if (!bullet.IsAlive)
                {
                    fireSound.Play();
                    bullet.IsAlive = true;
                    bullet.Position = this.Position + new Vector2(-2, -22);
                    bullet.direction = direction;
                    break;
                }
            }
        }

        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (State == EntityState.JUMPING)
            {
                // Begin or continue a jump
                if ((IsOnGround) || jumpTime > 0.0f)
                {
                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    sprite.PlayAnimation(jumpAnimation);
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

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
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
                    TileCollision collision = Level.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    IsOnGround = true;

                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || IsOnGround)
                                {
                                    // Resolve the collision along the Y axis.
                                    position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }


        /// <summary>
        /// When bowser get's hit. Called from a Bullet.
        /// </summary>
        /// <param name="damage">The damage.</param>
        /// <param name="hitFrom">The hit from.</param>
        public void Hit(float damage, int hitFrom)
        {
            this.health -= damage;
            pulseRed = true;
            pulseRedTime = 0.0f;

            gotHitFrom = hitFrom;

            hitSound.Play();

            if (this.health <= 0)
            {
                Killed();
                playDeathAnimation = true;
            }

        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (playDeathAnimation)
            {
                SpriteEffects flip;
                flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                flip = flip | SpriteEffects.FlipVertically;
                sprite.Draw(gameTime, spriteBatch, Position, flip);
            }
            else if (IsAlive)
            {
                // Draw that sprite.
                Color newColor = Color.White;

                if (pulseRed)
                {
                    Color spriteColor = Color.Red;
                    double speed = 30.0;
                    double pulseCycle = Math.Sin(gameTime.TotalGameTime.TotalSeconds * speed) / 2.0 + .5;
                    byte range = 100;

                    byte r = (byte)MathHelper.Clamp(MathHelper.Lerp((float)(spriteColor.R - range), (float)(spriteColor.R + range), (float)pulseCycle), 0, 255);
                    byte g = (byte)MathHelper.Clamp(MathHelper.Lerp((float)(spriteColor.G - range), (float)(spriteColor.G + range), (float)pulseCycle), 0, 255);
                    byte b = (byte)MathHelper.Clamp(MathHelper.Lerp((float)(spriteColor.B - range), (float)(spriteColor.B + range), (float)pulseCycle), 0, 255);

                    newColor.R = r;
                    newColor.G = g;
                    newColor.B = b;
                }

                // Draw facing the way the enemy is moving.
                SpriteEffects flip;
                flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                sprite.Draw(gameTime, spriteBatch, Position, flip, newColor);

                // Draw the fire attack
                foreach (BowserFire fire in _bullets)
                {
                    fire.Draw(gameTime, spriteBatch);
                }
            }
        }

    }
}
