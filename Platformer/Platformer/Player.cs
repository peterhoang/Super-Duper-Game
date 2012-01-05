#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

using ParticleEngine;

namespace Platformer
{   
    // When player gets hit
    public delegate void OnHitHandler(object sender, EventArgs e);

    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    public class Player
    {
        public event OnHitHandler OnHit;

        //font
        SpriteFont font;

        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;
        private Animation rollAnimation;
        private AnimationPlayer sprite;
        private SpriteEffects flip = SpriteEffects.None;
        public SpriteEffects Flip
        {
            get { return flip; }
        }

        public float GotHitFrom
        {
            get { return gotHitFrom; }
        }
        private float gotHitFrom;

        // Sounds
        private SoundEffect killedSound;
        private SoundEffect jumpSound;
        private SoundEffect fallSound;

        public PlatformerGame Level
        {
            get { return level; }
        }
        PlatformerGame level;

        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 12000.0f;
        private const float MaxMoveSpeed = 2000.0f;
        private const float GroundDragFactor = 0.65f;
        private const float AirDragFactor = 0.52f;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.3f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;
        private const float JumpControlPower = 0.14f;

        // Constants for rolling movement
        private const float MaxRollTime = 0.35f;
        private const float RollControlPower = 0.21f;
        private const float MaxRollRate = 0.6f;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;
        private const Buttons RollButton = Buttons.B;
        private const Buttons FireButton = Buttons.X;
        private const Buttons SwitchButton = Buttons.Y;

        public int PlayerId
        {
            get { return playerId; }
        }
        private int playerId;

        // Respawn point
        public Vector2 RespawnPosition
        {
            get { return respawnPosition; }
            set { respawnPosition = value; }
        }
        private Vector2 respawnPosition;

        //Health
        public float Health
        {
            get { return health; }
        }
        private float health;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround  
        {
            get { return isOnGround; }
        }
        bool isOnGround;

        public bool IsRolling
        {
            get { return isRolling; }
        }

        /// <summary>
        /// Current user movement input.
        /// </summary> 
        private float movement;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        // Rolling state
        private bool isRolling;
        private bool wasRolling;
        private bool canRollAgain = true;
        private float rollTime;
        private float rollRateTime;
        
        //Player's weapons
        private HandGun m_handgun;
        private Shotgun m_shotgun;
        private Gun gun;
        private float rateoffire;
        private const float HANDGUN_RATE = 0.2f;
        private const float SHOTGUN_RATE = 0.4f;
        private bool canShoot = true;

        //Sprite Effects: Pulse red
        private bool pulseRed;
        private float pulseRedTime;
        private const float MAX_PULSE_TIME = 0.4f;

        private GamePadState old_gamePadState = new GamePadState();

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
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

        /// <summary>
        /// Constructors a new player.
        /// </summary>
        public Player(PlatformerGame level, Vector2 position, int id)
        {
            this.level = level;
            this.playerId = id;

            LoadContent();

            respawnPosition = position;

            Reset(position);

        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/cop_idle"), 0.1f, true);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/cop_running"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/cop_jump"), 0.1f, false);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/cop_die"), 0.1f, false);
            rollAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/cop_roll2"), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerKilled");
            jumpSound = Level.Content.Load<SoundEffect>("Sounds/PlayerJump");
            fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerFall");

            m_handgun = new HandGun(level, position, this);
            m_shotgun = new Shotgun(level, position, this);

            gun = m_handgun;
            canShoot = true;

            font = level.Content.Load<SpriteFont>("Fonts/Hud");
        }

        public void LoadContent(string idle, string run, string jump, string death, string roll)
        {
            // Load animated textures.
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(idle), 0.1f, true);
            runAnimation = new Animation(Level.Content.Load<Texture2D>(run), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>(jump), 0.1f, false);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>(death), 0.1f, false);
            rollAnimation = new Animation(Level.Content.Load<Texture2D>(roll), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            isRolling = false;
            isJumping = false;
            pulseRed = false;
            pulseRedTime = 0.0f;
            health = 100;

            sprite.PlayAnimation(idleAnimation);
        }

        public void Reset()
        {
            Reset(respawnPosition);
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        /// <remarks>
        /// We pass in all of the input states so that our game is only polling the hardware
        /// once per frame. We also pass the game's orientation because when using the accelerometer,
        /// we need to reverse our motion when the orientation is in the LandscapeRight orientation.
        /// </remarks>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation,
            Viewport viewport)
        {
            GetInput(keyboardState, gamePadState, touchState, accelState, orientation);

            ApplyPhysics(gameTime);

            if (IsAlive && IsOnGround && !isRolling)
            {
                if (Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(runAnimation);
                }
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }

            rateoffire += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (gun == m_handgun)
            {
                if (rateoffire > HANDGUN_RATE)
                {
                    canShoot = true;
                    rateoffire = 0.0f;
                }
            }
            else if (gun == m_shotgun)
            {
                if (rateoffire > SHOTGUN_RATE)
                {
                    canShoot = true;
                    rateoffire = 0.0f;
                }
            }
            else
            {
                canShoot = false;
                rateoffire = 0.0f;
            }

            gun.Update(gameTime, Position, flip);
            
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

            // Clear input.
            movement = 0.0f;
            isJumping = false;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput(
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState,
            AccelerometerState accelState, 
            DisplayOrientation orientation)
        {
            // Get analog horizontal movement.
            movement = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement) < 0.5f)
                movement = 0.0f;

            // Move the player with accelerometer
            if (Math.Abs(accelState.Acceleration.Y) > 0.10f)
            {
                // set our movement speed
                movement = MathHelper.Clamp(-accelState.Acceleration.Y * AccelerometerScale, -1f, 1f);

                // if we're in the LandscapeLeft orientation, we must reverse our movement
                if (orientation == DisplayOrientation.LandscapeRight)
                    movement = -movement;
            }

            // If any digital horizontal movement input is found, override the analog movement.
            if (keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A))
            {
                movement = -1.0f;
            }
            else if (keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D))
            {
                movement = 1.0f;
            }

            // Weapon switching
            if (gamePadState.IsButtonDown(SwitchButton) && !old_gamePadState.IsButtonDown(SwitchButton))
            {
                gun.Reset();
                if (gun == m_handgun) 
                    gun = m_shotgun;
                else
                    gun = m_handgun;
            }
                     
                     
            // Check if the player wants to jump.
            isJumping =
                gamePadState.IsButtonDown(JumpButton) ||
                keyboardState.IsKeyDown(Keys.Space) ||
                keyboardState.IsKeyDown(Keys.Up) ||
                keyboardState.IsKeyDown(Keys.W) ||
                touchState.AnyTouch();

            if (gamePadState.IsButtonDown(RollButton) && !old_gamePadState.IsButtonDown(RollButton))
            {
                if (canRollAgain && isOnGround)
                {
                    isRolling = true;
                    canRollAgain = false;
                }
            }
            if ( (gamePadState.IsButtonDown(FireButton) && !old_gamePadState.IsButtonDown(FireButton)) ||
                 (keyboardState.IsKeyDown(Keys.Z)) )
            {
                if (canShoot)
                {
                    gun.Shoot();
                    canShoot = false;
                }
                    
            }

            old_gamePadState = gamePadState;
        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            velocity.X += movement * MoveAcceleration * elapsed;
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

            velocity.Y = DoJump(velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            velocity.X = DoRoll(velocity.X, gameTime);
           

            // Apply velocity.
            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // Player to player collision
            if (!isRolling)
            {
                foreach (Player player in PlatformerGame.Players)
                {
                    if (this != player)
                    {
                        if (player.IsAlive && this.BoundingRectangle.Intersects(player.BoundingRectangle))
                        {
                            float offset = (this.flip == SpriteEffects.None) ? -1.0f : 1.0f;
                            Position = new Vector2(previousPosition.X + offset, previousPosition.Y);
                        }
                    }
                }
            }

            // If the player is now colliding with the level, separate them.
            HandleCollisions();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (isJumping)
            {
                // Begin or continue a jump
                if ((!wasJumping && IsOnGround) || jumpTime > 0.0f)
                {
                    if (jumpTime == 0.0f)
                        jumpSound.Play();

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
            wasJumping = isJumping;

            return velocityY;
        }

        private float DoRoll(float velocityX, GameTime gameTime)
        {
            // If the player wants to jump
            if (isRolling)
            {
                // Begin or continue a roll
                if ((!wasRolling && IsOnGround) || rollTime > 0.0f)
                {
                    //if (rollTime == 0.0f)
                       // jumpSound.Play();

                    rollTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    sprite.PlayAnimation(rollAnimation);
                }

                // If we are in the middle of a roll
                if (0.0f < rollTime && rollTime <= MaxRollTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    float direction = (flip == SpriteEffects.FlipHorizontally) ? -1.0f : 1.0f;
                    velocityX = direction * (MaxMoveSpeed) * (1.0f - (float)Math.Pow(rollTime / MaxRollTime, RollControlPower));
                }
                else
                {
                    rollTime = 0.0f;
                    isRolling = false;
                }
               
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                rollTime = 0.0f;
            }
            wasRolling = isRolling;

            // Rate of rolling
            if (!canRollAgain)
            {
                rollRateTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (rollRateTime > MaxRollRate)
                {
                    rollRateTime = 0.0f;
                    canRollAgain = true;
                }
            }

            return velocityX;
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
            isOnGround = false;

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
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    isOnGround = true;

                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || IsOnGround)
                                {
                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

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
        /// When player get's hit. Called from a Bullet.
        /// </summary>
        /// <param name="damage">The damage.</param>
        /// <param name="hitFrom">The hit from.</param>
        public void Hit(float damage, float hitFrom, Player killedBy)
        {
            this.health -= damage;
            pulseRed = true;
            pulseRedTime = 0.0f;

            gotHitFrom = hitFrom;

            OnHit.Invoke(this, new EventArgs());
           
            if (this.health <= 0)
            {
                if (killedBy != null)
                    OnKilled(killedBy);
                else
                    OnKilled();
            }
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This parameter is null if the player was
        /// not killed by an enemy (fell into a hole).
        /// </param>
        public void OnKilled()
        {
            isAlive = false;
            pulseRed = false;
            pulseRedTime = 0.0f;
            killedSound.Play();

            gun.Reset();
            sprite.PlayAnimation(dieAnimation);
        }
        public void OnKilled(Player killedBy)
        {
            level.CurrentLevel.attacker_id = PlatformerGame.Players.IndexOf(killedBy);
          
            isAlive = false;
            pulseRed = false;
            pulseRedTime = 0.0f;
            killedSound.Play();
            
            gun.Reset();
            sprite.PlayAnimation(dieAnimation);
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit()
        {
            //sprite.PlayAnimation(celebrateAnimation);
        }

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Flip the sprite to face the way we are moving.
            if (Velocity.X > 0)
                flip = SpriteEffects.None;
            else if (Velocity.X < 0)
                flip = SpriteEffects.FlipHorizontally;               

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

           // spriteBatch.DrawString(font, Position.X.ToString(), position, Color.White);
            sprite.Draw(gameTime, spriteBatch, Position, flip, newColor);            

            // Draw the gun if not rolling
            if (isAlive) gun.Draw(gameTime, spriteBatch, isRolling);


            //if the player is offscreen, reset
            // Calculate the visible range of tiles
            int left = (int)Math.Floor(level.CurrentLevel.Camera.CameraPosition / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, level.CurrentLevel.Width - 1);
            Vector2 rightEdge = new Vector2(right, 0.0f) * Tile.Size;
            Vector2 leftEdge = new Vector2(left, 0.0f) * Tile.Size;

            if (position.X < leftEdge.X - (3 * Tile.Width) || position.X > rightEdge.X + (3 * Tile.Width))
            {
                level.CurrentLevel.StartNewLife(this, false);
            }
        }
    }
}
