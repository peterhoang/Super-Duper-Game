using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        public EntityState State { get; set; }

        private float jumpTime = 0.0f;

        public bool IsOnGround { get; set; }

        #endregion


        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Bowser(Level level, Vector2 position, string spriteSet)
            : base(level, position, spriteSet)
        {
            State = EntityState.IDLE;
        }
      
        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public override void LoadContent(string spriteSet)
        {
            // Load animations.
            spriteSet = "Sprites/" + spriteSet + "/";
            fireAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Fire"), 0.1f, true);

            base.LoadContent(spriteSet);
        }

        public override void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate tile position based on the side we are walking towards.
            float posX = Position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(Position.Y / Tile.Height);


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
            wasJumping = isJumping;

            return velocityY;
        }

    }
}
