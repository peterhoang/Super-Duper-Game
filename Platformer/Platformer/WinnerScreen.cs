#region Using Statements
using Microsoft.Xna.Framework;
#endregion

namespace GameStateManagement
{
    class WinnerScreen : MenuScreen
    {
        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public WinnerScreen()
            : base("Winner!")
        {
            // Create our menu entries.
            MenuEntry quitGameMenuEntry = new MenuEntry("Press A to Continue");
            
            // Hook up menu event handlers.
            quitGameMenuEntry.Selected += QuitGameMenuEntrySelected;

            MenuEntries.Add(quitGameMenuEntry);
        }


        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Quit Game menu entry is selected.
        /// </summary>
        void QuitGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, true, e.PlayerIndex,
                               new Platformer.PlatformerGame());
        }
        
        #endregion
    }
}
