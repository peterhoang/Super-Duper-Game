using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;

namespace Platformer
{
    class Shotgun : Weapon
    {
        private Animation baseGraphic;
        private AnimationPlayer sprite;

        private Animation muzzleFire;
        private AnimationPlayer muzzle;
        private float muzzleAnimationTimer;

        private float rateOfFire;
        private const float MAXFIRERATE = 0.4f;
        private bool canShoot = true;

        SoundEffect shotSound;

        private const int MAX_SHELLS = 2;
        public List<ShotgunShell> Shells
        {
            get { return _shells; }
        }
        protected List<ShotgunShell> _shells;

        public Shotgun(PlatformerGame level, Vector2 position, Player player)
        {
            this.game = level;
            Position = position;
            _player = player;

            // Initialize bullets
            _shells = new List<ShotgunShell>();
            for (int i = 0; i < MAX_SHELLS; i++)
            {
                _shells.Add(new ShotgunShell(level, position));
            }

            LoadContent();
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            baseGraphic = new Animation(Game.Content.Load<Texture2D>("Sprites/Weapons/shotgun"), 0.1f, false);
            muzzleFire = new Animation(Game.Content.Load<Texture2D>("Sprites/Weapons/shotgun_muzzle"), 0.01f, false);
            shotSound = Game.Content.Load<SoundEffect>("Sounds/shotgunBlastSound");
        }

        public override void Reset()
        {
            foreach (ShotgunShell shell in _shells)
            {
                shell.Reset();
            }
        }

        public override void Shoot()
        {
            if (!canShoot) return;

            //fire off a bullet if any available
            foreach (ShotgunShell shell in _shells)
            {
                if (!shell.IsAlive)
                {
                    shell.IsAlive = true;
                    shell.Position = this.Position;
                    shell.Flip = Flip;
                    shell.Player = _player;
                    isShooting = true;
                    canShoot = false;
                    shotSound.Play(0.7f, 0.0f, 0.0f);
                    break;
                }
            }
        }

        /// <summary>
        /// Update the gun position and animated muzzle fire
        /// </summary>
        public override void Update(GameTime gameTime, Vector2 position, SpriteEffects flip)
        {
            float elapsed =  (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 offset = (flip == SpriteEffects.None) ? new Vector2(37, 3) : new Vector2(-37, 3);
            Flip = flip;
            Position = position + offset;

            sprite.PlayAnimation(baseGraphic);

            if (isShooting)
            {
                muzzleAnimationTimer += elapsed;
                muzzle.PlayAnimation(muzzleFire);
                if (muzzleAnimationTimer > 0.20f)
                {
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
                    rateOfFire = 0.0f;
                    canShoot = true;
                }
            }

            // Update call for bullets
            foreach (ShotgunShell shell in _shells)
            {
                shell.Update(gameTime);
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
                Vector2 offset = (Flip == SpriteEffects.None) ? new Vector2(32, 2) : new Vector2(-32, 2);
                muzzle.Draw(gameTime, spriteBatch, Position + offset, Flip);
            }

        }

    }
}
