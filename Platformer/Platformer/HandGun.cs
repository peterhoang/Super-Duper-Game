using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;


namespace Platformer
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    class HandGun : Weapon
    {
        // graphic of the gun
        private Animation baseGraphic;
        private AnimationPlayer sprite;

        SoundEffect shotSound;

        // graphic of the muzzle 
        private Animation muzzleFire;
        private AnimationPlayer muzzle;
        private float muzzleAnimationTimer;

        private float rateOfFire;
        private const float MAXFIRERATE = 0.18f;
        private bool canShoot = true;

        private const int MAX_HANDGUN_BULLETS = 4;
        public List<HandgunBullet> Bullets
        {
            get { return _bullets; }
        }
        protected List<HandgunBullet> _bullets;

        public HandGun(PlatformerGame game, Vector2 position, Player player)
        {
            this.game = game;
            Position = position;
            _player = player;

            // Initialize bullets
            _bullets = new List<HandgunBullet>();
            for (int i = 0; i < MAX_HANDGUN_BULLETS; i++)
            {
                _bullets.Add(new HandgunBullet(game, position));
            }

            LoadContent();
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            baseGraphic = new Animation(Game.Content.Load<Texture2D>("Sprites/Weapons/handgun"), 0.1f, false);
            muzzleFire = new Animation(Game.Content.Load<Texture2D>("Sprites/Weapons/handgun_muzzle"), 0.03f, false);
            shotSound = Game.Content.Load<SoundEffect>("Sounds/pistolShotSound");
        }

        public override void Reset()
        {
            foreach (HandgunBullet bullet in _bullets)
            {
                bullet.Reset();
            }
        }

        public override void Shoot()
        {
            if (!canShoot) return;

            //fire off a bullet if any available
            foreach (HandgunBullet bullet in _bullets)
            {
                if (!bullet.IsAlive)
                {
                    bullet.IsAlive = true;
                    bullet.Position = this.Position + new Vector2(-2, -22);
                    bullet.Flip = Flip;
                    bullet.Player = _player;
                    isShooting = true;
                    canShoot = false;
                    shotSound.Play();
                    break;
                }
            }
        }

        /// <summary>
        /// Update the gun position and animated muzzle fire
        /// </summary>
        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(45, 0) : new Vector2(-45, 0);
            Flip = flip;
            Position = position + offset;

            sprite.PlayAnimation(baseGraphic);

            if (isShooting) {
                muzzleAnimationTimer += elapsed;
                muzzle.PlayAnimation(muzzleFire);
                if (muzzleAnimationTimer > 0.20f) {
                    muzzleAnimationTimer = 0.0f;
                    isShooting = false;
                    muzzle.StopAnimation();
                }
            }

            if (!canShoot)
            {
                rateOfFire += elapsed;
                if (rateOfFire > MAXFIRERATE)
                {
                    rateOfFire = 0f;
                    canShoot = true;
                }
            }

            // Update call for bullets
            foreach (HandgunBullet bullet in _bullets)
            {
                bullet.Update(gameTime);
            }
            
        }

        /// <summary>
        /// Draws the gun
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, Flip);

            if (isShooting)
            {
                Vector2 offset = (Flip == SpriteEffects.None) ? new Vector2(12, 0) : new Vector2(-12, 0);
                muzzle.Draw(gameTime, spriteBatch, Position + offset, Flip);
            }

            // Draw call for bullets
            foreach (HandgunBullet bullet in _bullets)
            {
                bullet.Draw(gameTime, spriteBatch);
            }
        }
    }
}
