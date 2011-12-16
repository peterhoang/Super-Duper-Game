#region File Description
//-----------------------------------------------------------------------------
// PlatformerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;
using System.Collections.Generic;
using ParticleEngine;

namespace Platformer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        public SpriteBatch SpriteBatch
        {
            get { return spriteBatch; }
        }
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        public Level CurrentLevel
        {
            get { return level; }
        }
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        // Represent the current controller states.
        GamePadState[] gamePadStates = new GamePadState[2];
        private GamePadState gamePadState_1;
        private GamePadState gamePadState_2;
        private KeyboardState keyboardState;
        private TouchCollection touchState;
        private AccelerometerState accelerometerState;
        
        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        private const int numberOfLevels = 3;

        // a random number generator that the whole sample can share.
        private static Random random = new Random(354669);
        public static Random Random
        {
            get { return random; }
        }

        
        // ************************** Particle System Stuff **********************************
        ExplosionParticleSystem explosion;
        ExplosionSmokeParticleSystem smoke;
        RetroBloodSprayParticleSystem bloodSprayRight;
        RetroBloodSprayParticleSystem bloodSprayLeft;
       
        // a timer that will tell us when it's time to trigger another explosion.
        const float TimeBetweenExplosions = 0.5f;
        float timeTillExplosion = 0.0f;

        // duration of the particle effect
        const float MAX_EXPLOSION_EFFECT_TIME = TimeBetweenExplosions;
        float timeExplosionEffect = 0.0f;
        bool startExplosionEffect = false;
        // ************************** Particle System Stuff **********************************


        public PlatformerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // create the particle systems and add them to the components list.
            // we should never see more than one explosion at once
            explosion = new ExplosionParticleSystem(this, 1);
            Components.Add(explosion);

            // but the smoke from the explosion lingers a while.
            smoke = new ExplosionSmokeParticleSystem(this, 2);
            Components.Add(smoke);

            bloodSprayRight = new RetroBloodSprayParticleSystem(this, 10);
            bloodSprayLeft = new RetroBloodSprayParticleSystem(this, 10);
            bloodSprayLeft.Dir = -1.0f;
            Components.Add(bloodSprayRight);
            Components.Add(bloodSprayLeft);


#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
            TargetElapsedTime = TimeSpan.FromTicks(333333);
#endif

            Accelerometer.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));
            }
            catch { }

            LoadNextLevel();

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Handle polling for our input and handling high-level input
            HandleInput();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // update our level, passing down the GameTime along with all of our input states
            level.Update(gameTime, keyboardState, gamePadStates, touchState, 
                         accelerometerState, Window.CurrentOrientation);

            UpdateExplosions(dt);
           
            base.Update(gameTime);
        }

        private void HandleInput()
        {
            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState_1 = GamePad.GetState(PlayerIndex.One);
            gamePadState_2 = GamePad.GetState(PlayerIndex.Two);
            gamePadStates[0] = gamePadState_1;
            gamePadStates[1] = gamePadState_2;
            touchState = TouchPanel.GetState();
            accelerometerState = Accelerometer.GetState();

            // Exit the game when back is pressed.
            if (gamePadState_1.Buttons.Back == ButtonState.Pressed)
                Exit();

            foreach (Player player in level.Players)
            {
                bool continuePressed =
                    keyboardState.IsKeyDown(Keys.Space) ||
                    gamePadStates[level.Players.IndexOf(player)].IsButtonDown(Buttons.A) ||
                    touchState.AnyTouch();

                // Perform the appropriate action to advance the game and
                // to get the player back to playing.
                if (!wasContinuePressed && continuePressed)
                {
                    if (!player.IsAlive)
                    {
                        level.StartNewLife(player);
                    }
                   // else if (level.TimeRemaining == TimeSpan.Zero)
                   // {
                   //     if (level.ReachedExit)
                   //         LoadNextLevel();
                   //     else
                   //         ReloadCurrentLevel();
                   // }

                }
                wasContinuePressed = continuePressed;
            }
            
        }

        // this function is called when we want to demo the explosion effect. it
        // updates the timeTillExplosion timer, and starts another explosion effect
        // when the timer reaches zero.
        private void UpdateExplosions(float dt)
        {
            if (!startExplosionEffect) return; 

            timeTillExplosion -= dt;
            if (timeTillExplosion < 0)
            {
                Vector2 where = Vector2.Zero;
             
                // the overall explosion effect is actually comprised of two particle
                // systems: the fiery bit, and the smoke behind it. add particles to
                // both of those systems.
                explosion.AddParticles(where);
                smoke.AddParticles(where);

                // reset the timer.
                timeTillExplosion = TimeBetweenExplosions;
            }

            // Update duration
            timeExplosionEffect += dt;
            if (timeExplosionEffect > MAX_EXPLOSION_EFFECT_TIME)
            {
                startExplosionEffect = false;
                timeExplosionEffect = 0.0f;
            }
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;

            // Unloads the content for the current level before loading the next one.
            if (level != null)
            {
                foreach (Player player in level.Players)
                {
                    player.OnHit -= player_OnHit;
                }
                level.Dispose();
            }

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            using (Stream fileStream = TitleContainer.OpenStream(levelPath)) {
                level = new Level(Services, fileStream, levelIndex);
                foreach (Player player in level.Players)
                {
                    player.OnHit += new OnHitHandler(player_OnHit);
                }
            }
        }

        /// <summary>
        /// Handles the OnHit event of the player control. For now, this is used to determine where to
        /// generate the particle effects
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_OnHit(object sender, EventArgs e)
        {
            Player player = sender as Player;
           
            float xRange = 10f;
            float yRange = 12f;
            float ycenter = player.Position.Y - 25;

            Vector2 where = Vector2.Zero;
            where.X = RandomBetween(player.Position.X - xRange, player.Position.X + xRange);
            where.Y = RandomBetween(ycenter - yRange, ycenter + yRange);

            if (player.GotHitFrom  > 0.0f)
                bloodSprayRight.AddParticles(where);
            else
                bloodSprayLeft.AddParticles(where);
     
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
           
            level.Draw(gameTime, spriteBatch);

            spriteBatch.Begin();
            
            DrawHud();

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            //spriteBatch.Begin();

            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            /*
            string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);
             *

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);
           
            // Determine the status overlay message to show.
            
            Texture2D status = null;
            /*
            foreach (Player player in level.Players)
            {
                if (level.TimeRemaining == TimeSpan.Zero)
                {
                    if (level.ReachedExit)
                    {
                        status = winOverlay;
                    }
                    else
                    {
                        status = loseOverlay;
                    }
                }
                else if (!player.IsAlive)
                {
                    status = diedOverlay;
                }
            }
             

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }
             * */

           // spriteBatch.End();
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }


        #region Helper Functions

        //  a handy little function that gives a random float between two
        // values. This will be used in several places in the sample, in particilar in
        // ParticleSystem.InitializeParticle.
        public static float RandomBetween(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        #endregion
    }
}
