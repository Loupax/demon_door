using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using XNAVERGE;

namespace DemonDoor {

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : VERGEGame {

        public McGrenderStack mcg;

        public Texture2D im_civvie, im_title, im_door, im_stage;
        public SpriteFont ft_hud24;

        private Screen _level;

        internal void LoadLevel(string level)
        {
            Screen s = null;

            switch (level)
            {
                case "title": s = new TitleScreen(); break;
                case "level1": s = new Level1Screen(); break;
            }

            _level = s;
            _level.Load();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            // TODO: Add your initialization logic here
            im_civvie = Content.Load<Texture2D>( "art/civilian_01" );
            im_title = Content.Load<Texture2D>( "art/title" );
            im_door = Content.Load<Texture2D>( "art/door" );
            im_stage = Content.Load<Texture2D>( "art/stage" );

            ft_hud24 = Content.Load<SpriteFont>("HUD24");

            LoadLevel("level1");

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            spritebatch = new SpriteBatch( GraphicsDevice );

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        int systime;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            {
                // update gun impulse.
                UpdateGunImpulse(gameTime);

                Vector2 dir = new Vector2 { X = -1, Y = 1 };
                dir.Normalize();

                _gun.Impulse = dir * GunImpulse;
            }

            _world.Simulate(gameTime);
            mcg.Update(gameTime);

            //Console.Out.WriteLine("@{3}: ({0}, {1}), {2}", _test.Position.X, _test.Position.Y, _test.Theta, gameTime.TotalGameTime);
            systime = gameTime.TotalGameTime.Milliseconds;

            // TODO: Add your update logic here

            _level.Update(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw( GameTime gameTime ) {
            GraphicsDevice.Clear( Color.LimeGreen );

            base.Draw( gameTime );

            // do level-specific drawing afterward so we can draw hud
            spritebatch.Begin();
            _level.Draw(spritebatch, gameTime);
            spritebatch.End();
        }
    }
}
