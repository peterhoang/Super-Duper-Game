using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using PrimitivesSample;

namespace Platformer
{
    class Grenade : Weapon
    {
        BasicEffect basicEffect;
        VertexPositionColor[] vertices;

        Animation idleAnimation;
        AnimationPlayer sprite;

        SoundEffect grenadeSound;
        SoundEffect explosionSound;

        Vector2 initalPosition;
        Vector2 acceleration = new Vector2(0, -9.8f);

        private bool isOnGround;
        private int previousBottom;

        public bool IsAttacking { get { return attacking; } }
        private bool attacking = false;
        private bool charging = false;
        private float attackTime = 0.0f;
        private const float MAXATTACKTIME = 0.3f;

        private bool canAttack = true;
        private float rateOfAttackTime = 0.0f;
        private const float MAXRATEOFATTACK = 1.5f;

        private Vector2 velocity;
        private const float BOMB_DAMAGE = 1.0f;

        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;

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
            basicEffect = new BasicEffect(game.ScreenManager.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter
               (0, game.ScreenManager.GraphicsDevice.Viewport.Width,     // left, right
                game.ScreenManager.GraphicsDevice.Viewport.Height, 0,    // bottom, top
                0, 1);                                         // near, far plane

            vertices = new VertexPositionColor[2];
            vertices[0].Position = new Vector3(100, 100, 0);
            vertices[0].Color = Color.Black;
            vertices[1].Position = new Vector3(200, 200, 0);
            vertices[1].Color = Color.Black;


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

        public void Charging(Vector2 position)
        {
            this.initalPosition = position;
            charging = true;
        }

        public override void Shoot()
        {
            charging = false;
            attacking = true;
        }

        public override void Reset()
        {
            attacking = false;
            charging = false;
            velocity = Vector2.Zero;
        }

        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (charging)
            {
                velocity.X += 10.0f;
                velocity.Y += 10.0f;
            }

            if (attacking)
            {
                this.position += velocity * elapsed - 0.5f * acceleration * elapsed * elapsed;

                rateOfAttackTime += elapsed;
                if (rateOfAttackTime > MAXRATEOFATTACK)
                {
                    attacking = false;
                    rateOfAttackTime = 0.0f;
                    this.Reset();
                }
            }
            else
            {
                Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(-20, -18) : new Vector2(20, -18);
                Flip = flip;
                this.position = position + offset;
            }


        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (attacking || charging)
                sprite.Draw(gameTime, spriteBatch, this.position, Flip);

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
            isOnGround = false;

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
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    isOnGround = true;

                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || isOnGround)
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
    }
}
