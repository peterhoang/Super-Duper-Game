#region File Description
//-----------------------------------------------------------------------------
// RetroBloodSprayWide.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

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
    /// <summary>
    /// RetroBloodSprayWide is a specialization of ParticleSystem which creates a
    /// fiery explosion. It should be combined with ExplosionSmokeParticleSystem for
    /// best effect.
    /// </summary>
    public class RetroBloodSprayWide : ParticleSystem
    {
        public RetroBloodSprayWide(PlatformerGame game, int howManyEffects)
            : base(game, howManyEffects)
        {
        }

        /// <summary>
        /// Set up the constants that will give this particle system its behavior and
        /// properties.
        /// </summary>
        protected override void InitializeConstants()
        {
            blendColor = Color.Red;

            textureFilename = "Sprites/quad";

            // high initial speed with lots of variance.  make the values closer
            // together to have more consistently circular explosions.
            minInitialSpeed = 100;
            maxInitialSpeed = 200;

            // doesn't matter what these values are set to, acceleration is tweaked in
            // the override of InitializeParticle.
            minAcceleration = -100;
            maxAcceleration = 300;

            // explosions should be relatively short lived
            minLifetime = 1.0f;
            maxLifetime = 1.5f;

            minScale = .1f;
            maxScale = .3f;

            // we need to reduce the number of particles on Windows Phone in order to keep
            // a good framerate
#if WINDOWS_PHONE
            minNumParticles = 10;
            maxNumParticles = 12;
#else
            minNumParticles = 150;
            maxNumParticles = 200;
#endif

            minRotationSpeed = -MathHelper.PiOver4;
            maxRotationSpeed = MathHelper.PiOver4;

            // additive blending is very good at creating fiery effects.
			blendState = BlendState.AlphaBlend;

            DrawOrder = AdditiveDrawOrder;
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
            direction.X = (float)Math.Cos(radians);
            direction.Y = -(float)Math.Sin(radians);
            return direction;
        }

        protected override void InitializeParticle(Particle p, Vector2 where)
        {
            base.InitializeParticle(p, where);

            p.Acceleration.Y += 100;
        }
    }
}
