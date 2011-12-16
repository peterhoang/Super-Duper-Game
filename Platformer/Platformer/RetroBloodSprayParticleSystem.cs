#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platformer;
#endregion

namespace ParticleEngine
{
    class RetroBloodSprayParticleSystem : ParticleSystem
    {
        public float Dir
        {
            get { return dir; }
            set { dir = value; }
        }
        private float dir = 1.0f;

        public RetroBloodSprayParticleSystem(PlatformerGame game, int howManyEffects)
            : base(game,howManyEffects)
        {
        }

         /// <summary>
        /// Set up the constants that will give this particle system its behavior and
        /// properties.
        /// </summary>
        protected override void InitializeConstants()
        {
            textureFilename = "Sprites/quad";
            blendColor = Color.Red;

            minInitialSpeed = 25;
            maxInitialSpeed = 160;

            // we don't want the particles to accelerate at all, aside from what we
            // do in our overriden InitializeParticle.
            minAcceleration = 0;
            maxAcceleration = 0;

            // long lifetime, this can be changed to create thinner or thicker smoke.
            // tweak minNumParticles and maxNumParticles to complement the effect.
            minLifetime = 0.2f;
            maxLifetime = 0.5f;

            minScale = .1f;
            maxScale = .3f;

            // we need to reduce the number of particles on Windows Phone in order to keep
            // a good framerate
#if WINDOWS_PHONE
            minNumParticles = 3;
            maxNumParticles = 8;
#else
            minNumParticles = 30;
            maxNumParticles = 60;
#endif

            // rotate slowly, we want a fairly relaxed effect
            minRotationSpeed = -MathHelper.PiOver4 * 2;
            maxRotationSpeed = MathHelper.PiOver4 * 2;

			blendState = BlendState.AlphaBlend;

            DrawOrder = AlphaBlendDrawOrder;
        }

        
        /// <summary>
        /// PickRandomDirection is overriden so that we can make the particles always 
        /// move have an initial velocity pointing up.
        /// </summary>
        /// <returns>a random direction which points basically up.</returns>
        protected override Vector2 PickRandomDirection()
        {
            // Point the particles somewhere between 80 and 100 degrees.
            // tweak this to make the smoke have more or less spread.
            float radians = PlatformerGame.RandomBetween(
                MathHelper.ToRadians(80), MathHelper.ToRadians(100));

            Vector2 direction = Vector2.Zero;
            // from the unit circle, cosine is the x coordinate and sine is the
            // y coordinate. We're negating y because on the screen increasing y moves
            // down the monitor.
            direction.X = dir * (float)Math.Sin(radians);
            direction.Y = (float)Math.Cos(radians) * 2;
            return direction;
        }

        /// <summary>
        /// InitializeParticle is overridden to add the appearance of wind.
        /// </summary>
        /// <param name="p">the particle to set up</param>
        /// <param name="where">where the particle should be placed</param>
        protected override void InitializeParticle(Particle p, Vector2 where)
        {
            base.InitializeParticle(p, where);

            // the base is mostly good, but we want to simulate a little bit of wind
            // heading to the right.
            // p.Acceleration.X += PlatformerGame.RandomBetween(10, 50);
            p.Acceleration.Y += PlatformerGame.RandomBetween(0, 100);
        }
    }

}
