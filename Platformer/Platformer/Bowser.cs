using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    public class Bowser : Enemy
    {
        Animation fireAnimation;

        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Bowser(Level level, Vector2 position, string spriteSet)
            : base(level, position, spriteSet)
        { }
      
        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public void LoadContent(string spriteSet)
        {
            // Load animations.
            spriteSet = "Sprites/" + spriteSet + "/";
            fireAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Fire"), 0.1f, true);

        }


    }
}
