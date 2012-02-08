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
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using ParticleEngine;

using GameStateManagement;
using System.Diagnostics;

namespace Platformer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : GameScreen
    {
        #region Fields

        // Resources for drawing.
        private GraphicsDevice graphics;
        public SpriteBatch SpriteBatch
        {
            get { return spriteBatch; }
        }
        private SpriteBatch spriteBatch;

        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;
        GameComponentCollection components; 

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;
        private Texture2D arrow;

        // Meta-level game state.
        private int levelIndex = 2;
        private Level level;
        public Level CurrentLevel
        {
            get { return level; }
        }
        //private bool wasContinuePressed;
        public bool firstKill = false;
        public static bool bossFight = false;

        // Player Entities in the game.
        public static List<Player> Players
        {
            get { return players; }
        }
        static List<Player> players = new List<Player>();
        
        // Attacker index
        public static int attacker_id = 0;

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
        private const int numberOfLevels = 7;
        private float levelTransitionDelayTime = 0.0f;
        private const float MAX_LEVEL_DELAY = 0.5f;
        private bool startLevelDelayTimer = false;

        // a random number generator that the whole sample can share.
        private static Random random = new Random(354669);
        public static Random Random
        {
            get { return random; }
        }

        float pauseAlpha;
        
        // ************************** Particle System Stuff **********************************
        ExplosionParticleSystem explosion;
        ExplosionSmokeParticleSystem smoke;
        RetroBloodSprayParticleSystem bloodSprayRight;
        RetroBloodSprayParticleSystem bloodSprayLeft;
        RetroBloodSprayWide bloodSprayUp;
       
        // a timer that will tell us when it's time to trigger another explosion.
        const float TimeBetweenExplosions = 0.5f;
        float timeTillExplosion = 0.0f;

        // duration of the particle effect
        const float MAX_EXPLOSION_EFFECT_TIME = TimeBetweenExplosions;
        float timeExplosionEffect = 0.0f;
        bool startExplosionEffect = false;
        // ************************** Particle System Stuff **********************************

        // ************************************ Sounds ***************************************
        private static SoundEffect firstBloodSound;
        private static SoundEffect dominatingSound;
        private static SoundEffect doublekillSound;
        private static SoundEffect godlikeSound;
        private static SoundEffect holyshitSound;
        private static SoundEffect killingspreeSound;
        private static SoundEffect megakillSound;
        private static SoundEffect monsterKillSound;
        private static SoundEffect multikillSound;
        private static SoundEffect ultrakillSound;
        private static SoundEffect unstoppableSound;
        private static SoundEffect wickedsickSound;
        private static SoundEffect failSound;
        // ***********************************************************************************

        #endregion

        #region Initialize

        public PlatformerGame()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
            TargetElapsedTime = TimeSpan.FromTicks(333333);
#endif
            try
            {
                Accelerometer.Initialize();
            }
            catch { }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            if (components == null)
                components = ScreenManager.Game.Components;

            if (graphics == null)
                graphics = ScreenManager.GraphicsDevice;

            // create the particle systems and add them to the components list.
            // we should never see more than one explosion at once
            explosion = new ExplosionParticleSystem(this, 1);
            components.Add(explosion);

            // but the smoke from the explosion lingers a while.
            smoke = new ExplosionSmokeParticleSystem(this, 2);
            components.Add(smoke);

            bloodSprayUp = new RetroBloodSprayWide(this, 100);
            bloodSprayRight = new RetroBloodSprayParticleSystem(this, 10);
            bloodSprayLeft = new RetroBloodSprayParticleSystem(this, 10);
            bloodSprayLeft.Dir = -1.0f;
            components.Add(bloodSprayUp);
            components.Add(bloodSprayRight);
            components.Add(bloodSprayLeft);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(ScreenManager.Game.GraphicsDevice);

            // Load fonts
            hudFont = content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay = content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = content.Load<Texture2D>("Overlays/you_died");
            arrow = content.Load<Texture2D>("Sprites/arrow");

            // Load sounds
            firstBloodSound = content.Load<SoundEffect>("Sounds/UT/firstblood");
            dominatingSound = content.Load<SoundEffect>("Sounds/UT/dominating");
            doublekillSound = content.Load<SoundEffect>("Sounds/UT/doublekill");
            godlikeSound = content.Load<SoundEffect>("Sounds/UT/godlike");
            holyshitSound = content.Load<SoundEffect>("Sounds/UT/holyshit");
            killingspreeSound = content.Load<SoundEffect>("Sounds/UT/killingspree");
            megakillSound = content.Load<SoundEffect>("Sounds/UT/megakill");
            monsterKillSound = content.Load<SoundEffect>("Sounds/UT/monsterkill");
            multikillSound = content.Load<SoundEffect>("Sounds/UT/multikill");
            ultrakillSound = content.Load<SoundEffect>("Sounds/UT/ultrakill");
            unstoppableSound = content.Load<SoundEffect>("Sounds/UT/unstoppable");
            wickedsickSound = content.Load<SoundEffect>("Sounds/UT/wickedsick");
            failSound = content.Load<SoundEffect>("Sounds/smb_gameover");

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(content.Load<Song>("Sounds/Music/bgmusic1"));
            }
            catch { }

            //Create the players
            //Player 1
            players.Add(new Player(this, Vector2.Zero, 0));
            players.Add(new Player(this, Vector2.Zero, 1));

              // Set player 2's colors
            players[1].LoadContent("Sprites/Player/cop_yellow_idle",
                    "Sprites/Player/cop_yellow_running",
                    "Sprites/Player/cop_yellow_jump",
                    "Sprites/Player/cop_yellow_die",
                    "Sprites/Player/cop_yellow_roll",
                    "Sprites/Player/cop_yellow_grenade");
            players[1].Flip = SpriteEffects.FlipHorizontally;

            LoadNextLevel();

        }

        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            foreach (Player player in Players)
            {
                player.OnHit -= player_OnHit;
            }
            Players.Clear();
            bossFight = false;
            content.Unload();  
        }

        #endregion

        #region Update & Draw

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            if (startLevelDelayTimer)
            {
                levelTransitionDelayTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (levelTransitionDelayTime > MAX_LEVEL_DELAY)
                {
                    levelTransitionDelayTime = 0.0f;
                    startLevelDelayTimer = false;
                }
            }
            else
            {

                base.Update(gameTime, otherScreenHasFocus, false);
                // Handle polling for our input and handling high-level input

                // Gradually fade in or out depending on whether we are covered by the pause screen.
                if (coveredByOtherScreen)
                    pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
                else
                    pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);

                if (IsActive)
                {
                    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                    // update our level, passing down the GameTime along with all of our input states
                    level.Update(gameTime, keyboardState, gamePadStates, touchState,
                                 accelerometerState, ScreenManager.Game.Window.CurrentOrientation, graphics.Viewport);

                    UpdateExplosions(dt);
                }
            }
           
        }

        public override void HandleInput(InputState input)
        {
            if (input == null)
               throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState_1 = GamePad.GetState(PlayerIndex.One);
            gamePadState_2 = GamePad.GetState(PlayerIndex.Two);
            gamePadStates[0] = gamePadState_1;
            gamePadStates[1] = gamePadState_2;
            touchState = TouchPanel.GetState();
            accelerometerState = Accelerometer.GetState();

            // Exit the game when back is pressed.
            //if (gamePadState_1.Buttons.Back == ButtonState.Pressed)
            //    Exit();

            if (input.IsPauseGame(null))
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                foreach (Player player in PlatformerGame.Players)
                {
                    bool continuePressed =
                        keyboardState.IsKeyDown(Keys.Space) ||
                        gamePadStates[PlatformerGame.Players.IndexOf(player)].IsButtonDown(Buttons.A) ||
                        touchState.AnyTouch();

                    if (!player.IsAlive)
                    {
                        level.StartNewLife(player);
                    }
                    if (level.ReachedExit)
                    {
                        LoadNextLevel();
                    }

                }
                //check if both player is in the "no-man lands"
                if (players[0].Position.X == -999.9f && players[1].Position.X == -999.9f)
                {
                    //players[attacker_id].Reset(Vector2.Zero);
                    level.StartNewLife(players[attacker_id]);
                }
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
            if (level != null)
            {
                levelIndex = (PlatformerGame.attacker_id == 0) ? (levelIndex + 1) % numberOfLevels : (levelIndex - 1) % numberOfLevels;
            }
            else
            {
                levelIndex = (levelIndex + 1) % numberOfLevels;
            }

            // Provide the background set
            string[] backgroundSet = new string[3] { "Backgrounds/Background0_0", "Backgrounds/Background0_1", "Backgrounds/Background0_2" };
            switch (levelIndex)
            {
                case 2:
                case 4:
                    backgroundSet[0] = "Backgrounds/Background1_0";
                    backgroundSet[1] = "Backgrounds/Background1_1";
                    backgroundSet[2] = "Backgrounds/Background1_2";
                    break;
                case 3:
                    backgroundSet[0] = "Backgrounds/Background0_0";
                    backgroundSet[1] = "Backgrounds/Background0_1";
                    backgroundSet[2] = "Backgrounds/Background0_2";
                    break;
                case 1:
                case 5:
                    backgroundSet[0] = "Backgrounds/Background2_0";
                    backgroundSet[1] = "Backgrounds/Background2_1";
                    backgroundSet[2] = "Backgrounds/Background2_2";
                    break;
                case 0:
                case 6:
                    backgroundSet[0] = null;
                    backgroundSet[1] = null;
                    backgroundSet[2] = null;
                    break;
            }

            //If the either reaches the last level, remove the other player  
            if (levelIndex == 6 || levelIndex == 0)
            {
                int idx = (attacker_id == 0) ? 1 : 0;
                Players[idx].IsRespawnable = false;
                Players[idx].Reset(Vector2.Zero);
                bossFight = true;
                try
                {
                    MediaPlayer.Stop();
                    MediaPlayer.Play(content.Load<Song>("Sounds/Music/smb-castle"));
                }
                catch { }
            }
      

            // Unloads the content for the current level before loading the next one.
            if (level != null)
            {
                foreach (Player player in PlatformerGame.Players)
                {
                    player.OnHit -= player_OnHit;
                    player.IsRespawnable = true;
                }
                level.Dispose();
            }

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            using (Stream fileStream = TitleContainer.OpenStream(levelPath)) {
                level = new Level(ScreenManager.Game.Services, fileStream, levelIndex, backgroundSet, this);
                foreach (Player player in PlatformerGame.Players)
                {
                    player.OnHit += new OnHitHandler(player_OnHit);
                }
            }

            startLevelDelayTimer = true;
        }

       
        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            graphics.Clear(Color.CornflowerBlue);
           
            level.Draw(gameTime, spriteBatch);

            spriteBatch.Begin();
            
            DrawHud(gameTime);

            spriteBatch.End();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }

            base.Draw(gameTime);
        }

        
        private void DrawHud(GameTime gameTime)
        {
            //spriteBatch.Begin();

            Rectangle titleSafeArea = ScreenManager.Game.GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 rightMargin = new Vector2(titleSafeArea.X + titleSafeArea.Width * 0.93f, titleSafeArea.Y);
            Vector2 leftMargin = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            if (firstKill && (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                if (PlatformerGame.attacker_id == 0)
                    spriteBatch.Draw(arrow, rightMargin, Color.Blue);
                else if (PlatformerGame.attacker_id == 1)
                    spriteBatch.Draw(arrow, leftMargin, arrow.Bounds, Color.Yellow, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.FlipHorizontally, 0.0f);
            }

            //spriteBatch.DrawString(hudFont, level.Camera.CameraPosition.ToString(), leftMargin, Color.White);

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
            foreach (Player player in PlatformerGame.Players)
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

        #endregion


        #region Helper Functions

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

            if (player.GotHitFrom > 0.0f)
                bloodSprayRight.AddParticles(where);
            else
                bloodSprayLeft.AddParticles(where);

            if (player.Health <= 0.0f)
            {
                bloodSprayUp.AddParticles(new Vector2(player.Position.X, player.Position.Y + 10));
                if (!firstKill)
                {
                    firstBloodSound.Play();
                    firstKill = true;
                }

                if (bossFight)
                {
                    displayEpicFail();
                }
                
            }
        }
        public void displayEpicFail()
        {
            const string message = "EPIC FAIL!!!!!1";

            CustomMessageBoxScreen confirmQuitMessageBox = new CustomMessageBoxScreen(message, false);

            confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmQuitMessageBox, ControllingPlayer);

            try
            {
                MediaPlayer.Stop();
                failSound.Play();
            }
            catch { }

        }
        void ConfirmQuitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, true, e.PlayerIndex, new PlatformerGame(), new CountDownScreen());
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }


        //  a handy little function that gives a random float between two
        // values. This will be used in several places in the sample, in particilar in
        // ParticleSystem.InitializeParticle.
        public static float RandomBetween(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
        public static int RandomBetween(int min, int max)
        {
            return random.Next(min, max);
        }

        public static void PlayUTSounds(int killcount)
        {
            if (killcount == 2)
            {
                doublekillSound.Play();
            }
            else if (killcount == 3)
            {
                multikillSound.Play();
            }
            else if (killcount >= 4)
            {
                int rand = RandomBetween(1, 200);
                if (rand > 0 && rand < 25)
                {
                    holyshitSound.Play();
                }
                else if (rand >= 25 && rand < 50)
                {
                    dominatingSound.Play();
                }
                else if (rand >= 50 && rand < 75)
                {
                    killingspreeSound.Play();
                }
                else if (rand >= 75 && rand < 100)
                {
                    megakillSound.Play();
                }
                else if (rand >= 100 && rand < 125)
                {
                    godlikeSound.Play();
                }
                else if (rand >= 125 && rand < 150)
                {
                    unstoppableSound.Play();
                }
                else if (rand >= 150 && rand < 175)
                {
                    monsterKillSound.Play();
                }
                else if (rand >= 175)
                {
                    ultrakillSound.Play();
                }
            }
        }
        #endregion
    }
}
