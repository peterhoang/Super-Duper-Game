﻿#region File Description
//-----------------------------------------------------------------------------
// Level.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;

using ParticleEngine;
using System.Diagnostics;
using GameStateManagement;
using Microsoft.Xna.Framework.Media;

namespace Platformer
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    public class Level : IDisposable
    {
        // Physical structure of the level.
        private Tile[,] tiles;
        private Tile[,] bridgeTiles;
        //private Texture2D[] layers;
        private Layer[] layers;

        // The layer which entities are drawn on top of.
        private const int EntityLayer = 2;

        private List<Gem> gems = new List<Gem>();
        private List<Spike> spikes = new List<Spike>();
        private List<WinnerTile> winnerTiles = new List<WinnerTile>();
        public List<Enemy> Enemies { get { return enemies; } }
        private List<Enemy> enemies = new List<Enemy>();

        private float withdrawBridgeWaitTime = 1.0f;
        private bool isBridgeWithdrawing = false;
        
        private const int MAX_CORPSES = 10;
        private Corpse[,] corpses = new Corpse[2,MAX_CORPSES];
        private int[] corpseIndex = new int[2];
       
        // Key locations in the level.        
        private Vector2 start;
        private Point[] exit = new Point[2];
        private Point bridgeSwitch;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state.
        private Random random = new Random(354668); // Arbitrary, but constant seed

        public PlatformerGame Game
        {
            get { return game; }
        }
        private PlatformerGame game;

        public Camera2d Camera
        {
            get { return camera; }
        }
        private Camera2d camera;
        
        public int Score
        {
            get { return score; }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;

        private const int PointsPerSecond = 5;

        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        //Sounds
        private SoundEffect fallingSound;
        private SoundEffect clearSound;
        private SoundEffect bridgeSound;        

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex, string[] backgroundSet, PlatformerGame game)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            this.game = game;

            timeRemaining = TimeSpan.FromMinutes(2.0);
            exit[0] = InvalidPosition;
            exit[1] = InvalidPosition;
            
            LoadTiles(fileStream);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Layer[3];
            float scrollRate = 0.2f;
            for (int i = 0; i < layers.Length; ++i)
            {
                layers[i] = new Layer(Content, backgroundSet[i], scrollRate);
                scrollRate += 0.3f;
            }
            /*
            for (int i = 0; i < layers.Length; ++i)
            {
                // Choose a random segment if each background layer for level variety.
                int segmentIndex = levelIndex;
                layers[i] = Content.Load<Texture2D>("Backgrounds/Layer" + i + "_" + segmentIndex);
            }
             * */

            // Load sounds.
            fallingSound = Content.Load<SoundEffect>("Sounds/fallSound");
            clearSound = Content.Load<SoundEffect>("Sounds/smb_stage_clear");
            bridgeSound = content.Load<SoundEffect>("Sounds/bridgeSound");

            camera = new Camera2d(this, 0.0f);

            // allocate for corpses
            corpseIndex[0] = 0;
            corpseIndex[1] = 0;
            for (int i = 0; i < MAX_CORPSES; i++)
            {
                corpses[0, i] = new Corpse(this, Vector2.Zero, "Sprites/Player/blue_corpse");
                corpses[1, i] = new Corpse(this, Vector2.Zero, "Sprites/Player/yellow_corpse");
            }
           
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];
            bridgeTiles = new Tile[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            if (PlatformerGame.Players.Count <= 0)
                throw new NotSupportedException("A level must have a starting point.");
        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);
                case '|':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile1(x, y);
                case 'Q':
                    return LoadExitTile2(x, y);

                // Gem
                case 'G':
                    return LoadGemTile(x, y);

                // Floating platform
                case '-':
                    return LoadTile("Platform", TileCollision.Platform);

                // Various enemies
                case 'A':
                    return LoadEnemyTile(x, y, "MonsterA");
                case 'B':
                    return LoadEnemyTile(x, y, "MonsterB");
                case 'D':
                    return LoadEnemyTile(x, y, "MonsterD");
                case 'E':
                    return LoadBowserTile(x, y, "Bowser");

                // Platform block
                case '~':
                    return LoadBridgeTile(x, y, TileCollision.Platform);

                // Passable block
                case ':':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);
                case 'C':
                    return LoadTile("chain", TileCollision.Passable);
                case 'c':
                    return LoadTile("chain_flipped", TileCollision.Passable);
                case 'W':
                    return LoadWinnerTile(x, y);

                // Player 1 start point
                case '1':
                    if (PlatformerGame.attacker_id == 0 && game.firstKill)
                        return LoadStartTile(x, y, 0);
                    else
                        return new Tile(null, TileCollision.Passable);
                // Player 2 start point
                case '2':
                    if (PlatformerGame.attacker_id == 0 && game.firstKill)
                        return LoadStartTile(x, y, 1);
                    else
                        return new Tile(null, TileCollision.Passable);
                // Player 1 alternative start point
                case '3':
                    if (PlatformerGame.attacker_id == 1 && game.firstKill)
                        return LoadStartTile(x, y, 0);
                    else
                        return new Tile(null, TileCollision.Passable);
                // Player 2 alternative start point
                case '4':
                    if (PlatformerGame.attacker_id == 1 && game.firstKill)
                        return LoadStartTile(x, y, 1);
                    else
                        return new Tile(null, TileCollision.Passable);
                case '5':
                    if (!game.firstKill)
                        return LoadStartTile(x, y, 0);
                    else
                        return new Tile(null, TileCollision.Passable);
                case '6':
                    if (!game.firstKill)
                        return LoadStartTile(x, y, 1);
                    else
                        return new Tile(null, TileCollision.Passable);

                case '^':
                    return LoadSpikeTile(x, y);

                // Impassable block
                case '#':
                    return LoadTile("block-87", TileCollision.Impassable);
                case '%':
                    return LoadTile("block-87-red", TileCollision.Impassable);
                case '@':
                    return LoadTile("block-87-purp", TileCollision.Impassable);
                case '$':
                    return LoadTile("block-3", TileCollision.Impassable);
                case '&':
                    return LoadTile("greyblock1", TileCollision.Impassable);
                case 'L':
                    return LoadTile("lavatop", TileCollision.Passable);
                case 'T':
                    return LoadBridgeSwitchTile(x, y);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        private Tile LoadBridgeTile(int x, int y, TileCollision collision)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            Tile bridgeTile = new Tile(Content.Load<Texture2D>("Tiles/bridge"), collision);
            bridgeTiles[x, y] = bridgeTile;

            return bridgeTiles[x, y];
        }


        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }


        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y, int id)
        {
            //if (Player != null)
            //    throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            PlatformerGame.Players[id].Reset(start);

            return new Tile(null, TileCollision.Passable);
        }

      
        /// <summary>
        /// Remembers the location of the level's exit for player 1
        /// </summary>
        private Tile LoadExitTile1(int x, int y)
        {
            //if (exit != InvalidPosition)
            //    throw new NotSupportedException("A level may only have one exit.");

            exit[0] = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit for player 2
        /// </summary>
        private Tile LoadExitTile2(int x, int y)
        {
            //if (exit != InvalidPosition)
            //    throw new NotSupportedException("A level may only have one exit.");

            exit[1] = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        private Tile LoadBridgeSwitchTile(int x, int y)
        {
            bridgeSwitch = GetBounds(x, y).Center;
            return LoadTile("handle", TileCollision.Passable);
        }

        private Tile LoadWinnerTile(int x, int y)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            winnerTiles.Add(new WinnerTile(this, position));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a Bowser and puts him in the level.
        /// </summary>
        private Tile LoadBowserTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Bowser(this, position, spriteSet));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadSpikeTile(int x, int y)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            spikes.Add(new Spike(this, position));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadGemTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Gem(this, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState[] gamePadStates, 
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation, 
            Viewport viewport)
        {
            // Play the bridge withdrawing animation
            if (isBridgeWithdrawing)
            {
                WithdrawBridge(gameTime);
            }
            else
            {

            // Pause while the player is dead or time is expired.
                foreach (Player player in PlatformerGame.Players)
                {
                    if (!player.IsAlive)// || TimeRemaining == TimeSpan.Zero)
                    {
                        // Still want to perform physics on the player.
                        player.ApplyPhysics(gameTime);
                    }
                    else if (ReachedExit)
                    {
                        // Animate the time being converted into points.
                        int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                        seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                        timeRemaining -= TimeSpan.FromSeconds(seconds);
                        score += seconds * PointsPerSecond;
                    }
                    else
                    {
                        timeRemaining -= gameTime.ElapsedGameTime;
                        player.Update(gameTime, keyboardState, gamePadStates[player.PlayerId], touchState, accelState, orientation, viewport);
                        UpdateGems(gameTime);

                        // Falling off the bottom of the level kills the player.
                        if (player.BoundingRectangle.Top >= Height * Tile.Height)
                        {
                            PlatformerGame.attacker_id = (player.PlayerId == 0) ? 1 : 0;
                            fallingSound.Play();

                            if (!PlatformerGame.bossFight)
                            {
                                player.OnKilled();
                                player.Reset();
                            }
                            else
                            {
                                game.displayEpicFail();
                            }
                            
                        }

                        UpdateEnemies(gameTime);
                        UpdateSpikes(gameTime);
                        UpdateWinnerTile(gameTime);

                        // The player has reached the exit if they are standing on the ground and
                        // his bounding rectangle contains the center of the exit tile. They can only
                        // exit when they have collected all of the gems.
                        if (game.firstKill && player.PlayerId == PlatformerGame.attacker_id)
                        {
                            if (player.IsAlive && player.IsOnGround)
                            {
                                if (player.BoundingRectangle.Contains(exit[PlatformerGame.attacker_id]))
                                {
                                    OnExitReached();
                                }
                                if (player.BoundingRectangle.Contains(bridgeSwitch))
                                {
                                    WithdrawBridge(gameTime);
                                }
                            }


                        }
                    }
                }
            }

            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
            {
                //timeRemaining = TimeSpan.Zero;
                timeRemaining = TimeSpan.FromMinutes(2.0);
            }
        }

        /// <summary>
        /// Animates each gem and checks to allows the player to collect them.
        /// </summary>
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Gem gem = gems[i];

                gem.Update(gameTime);

                foreach (Player player in PlatformerGame.Players)
                {
                    if (gem.BoundingCircle.Intersects(player.BoundingRectangle))
                    {
                        gems.RemoveAt(i--);
                        OnGemCollected(gem, player);
                    }
                }
            }
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {
            // Calculate the visible range of tiles
            int left = (int)Math.Floor(camera.CameraPosition / Tile.Width);
            int right = left + game.ScreenManager.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);


            foreach (Enemy enemy in enemies)
            {
                if (enemy.Position.X / Tile.Width > left && enemy.Position.X / Tile.Width < right)
                {

                    enemy.Update(gameTime);
                    // Falling off the bottom of the level kills the enemy.
                    if (enemy.BoundingRectangle.Top >= Height * Tile.Height)
                    {
                        if (enemy.GetType() == typeof(Bowser))
                        {
                            Bowser bowser = enemy as Bowser;
                            if (bowser.IsAlive)
                            {
                                bowser.Killed();
                            }

                        }
                    }
                    
                    foreach (Player player in PlatformerGame.Players)
                    {
                        // Touching an enemy instantly kills the player
                        if (enemy.BoundingRectangle.Intersects(player.BoundingRectangle))
                        {
                            player.Hit(50.0f, (float)enemy.direction, null);
                        }
                    }
                    
                }
            }
        }

        private void UpdateWinnerTile(GameTime gameTime)
        {
            foreach (WinnerTile winning in winnerTiles)
            {
                if (winning.BoundingRectangle.Intersects(PlatformerGame.Players[PlatformerGame.attacker_id].BoundingRectangle))
                {
                    const string message = "WINNER!";

                    CustomMessageBoxScreen confirmQuitMessageBox = new CustomMessageBoxScreen(message, false);

                    confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

                    game.ScreenManager.AddScreen(confirmQuitMessageBox, game.ControllingPlayer);

                    try
                    {
                        MediaPlayer.Stop();
                        clearSound.Play();
                    }
                    catch { }

                }

            }
        }
        void ConfirmQuitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(game.ScreenManager, true, e.PlayerIndex, new PlatformerGame(), new CountDownScreen());
        }

        /// <summary>
        /// Collision detection for the spikes.
        /// </summary>
        private void UpdateSpikes(GameTime gameTime)
        {
            foreach (Spike spike in spikes)
            {
                foreach (Player player in PlatformerGame.Players)
                {
                    // Touching a spike instantly kills the player
                    if (spike.BoundingRectangle.Intersects(player.BoundingRectangle))
                    {
                        PlatformerGame.attacker_id = (player.PlayerId == 0) ? 1 : 0;
                        player.Hit(100, 1.0f, null);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnGemCollected(Gem gem, Player collectedBy)
        {
            score += Gem.PointValue;

            gem.OnCollected(collectedBy);
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            foreach (Player player in PlatformerGame.Players)
            {
                player.OnReachedExit();
                reachedExit = true;
            }
        }


        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife(Player player)
        {
            StartNewLife(player, true);
        }
        public void StartNewLife(Player player, bool isKilled)
        {
            float xpos = camera.GetSpawnPoint(game.ScreenManager.GraphicsDevice.Viewport);
            if (isKilled)
            {
                SpawnCorpse(player.Position, player.Flip, PlatformerGame.Players.IndexOf(player));
            }
            if (xpos > 0.0f)
            {
                //player.IsRespawnable = true;
                float ypos = -0.0f;
                player.Reset(new Vector2(xpos, ypos));
            }
            //else if (xpos == -999.9f)
            //{
                //player.IsRespawnable = false;
            //    player.Reset(new Vector2(xpos, 0.0f));
            //}
        }

        public void SpawnCorpse(Vector2 pos, SpriteEffects flip, int playerIndex)
        {
            corpses[playerIndex, corpseIndex[playerIndex]].IsActive = true;
            corpses[playerIndex, corpseIndex[playerIndex]].Position = pos;
            corpses[playerIndex, corpseIndex[playerIndex]].Flip = flip; 

            corpseIndex[playerIndex] = (corpseIndex[playerIndex] + 1 >= MAX_CORPSES) ? 0 : corpseIndex[playerIndex] + 1;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            for (int i = 0; i <= EntityLayer; ++i)
            {
                //spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);
                layers[i].Draw(spriteBatch, camera.CameraPosition);
            }
            spriteBatch.End();

            // center camera on level init
            if (!game.firstKill)
            {
                float approxCenter = ((float)(Width * Tile.Width) / 2.0f) + (Tile.Width * 2.6f);
                Vector2 centerScreen = new Vector2(approxCenter, 0.0f);
                camera.ScrollCamera(centerScreen, spriteBatch.GraphicsDevice.Viewport);
            }
            else
            {
                camera.ScrollCamera(PlatformerGame.Players[PlatformerGame.attacker_id].Position, spriteBatch.GraphicsDevice.Viewport);
            }
            
            Matrix cameraTransform = Matrix.CreateTranslation(-camera.CameraPosition, 0.0f, 0.0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default,
                              RasterizerState.CullCounterClockwise, null, cameraTransform);

            DrawTiles(spriteBatch);

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < MAX_CORPSES; i++)
                {
                    if (corpses[j, i].IsActive)
                        corpses[j, i].Draw(gameTime, spriteBatch);
                }
            }

            foreach (Gem gem in gems)
                gem.Draw(gameTime, spriteBatch);

            foreach (Player player in PlatformerGame.Players)
                player.Draw(gameTime, spriteBatch);
            
            DrawEnemies(gameTime, spriteBatch);

            foreach (Spike spike in spikes)
                spike.Draw(gameTime, spriteBatch);

            foreach (WinnerTile tile in winnerTiles)
                tile.Draw(gameTime, spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();
            for (int i = EntityLayer + 1; i < layers.Length; ++i)
            {
                //spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);
                layers[i].Draw(spriteBatch, camera.CameraPosition);
            }
            spriteBatch.End();
        }

        private void DrawEnemies(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles
            int left = (int)Math.Floor(camera.CameraPosition / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);

            foreach (Enemy enemy in enemies)
            {
                if (enemy.Position.X / Tile.Width > left && enemy.Position.X / Tile.Width < right)
                {
                    enemy.Draw(gameTime, spriteBatch);
                }
            }
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles
            int left = (int)Math.Floor(camera.CameraPosition / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);

            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Animates the withdrawal of the bridge
        /// </summary>
        private void WithdrawBridge(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            withdrawBridgeWaitTime += elapsed;

            isBridgeWithdrawing = true;

            if (withdrawBridgeWaitTime > 0.1f) 
            {
                // Calculate the visible range of tiles
                int left = (int)Math.Floor(camera.CameraPosition / Tile.Width);
                int right = left + game.ScreenManager.GraphicsDevice.Viewport.Width / Tile.Width;
                right = Math.Min(right, Width - 1);

                bool isThereMoreLeft = false;
                for (int y = 0; y < Height; ++y)
                {
                    for (int x = left; x <= right; ++x)
                    {
                        if (bridgeTiles[x, y].Texture != null && tiles[x, y].Texture != null)
                        {
                            if (tiles[x, y].Texture.Name == bridgeTiles[x, y].Texture.Name)
                            {
                                tiles[x, y].Texture = null;
                                tiles[x, y].Collision = TileCollision.Passable;

                                bridgeTiles[x, y].Texture = null;
                                bridgeTiles[x, y].Collision = TileCollision.Passable;
                               
                                isThereMoreLeft = true;
                                bridgeSound.Play();
                                withdrawBridgeWaitTime = 0.0f;
                                break;
                            }
                        }
                    }
                    if (isThereMoreLeft) break;
                }
                if (!isThereMoreLeft) isBridgeWithdrawing = false;
            }
        }
    }
}
