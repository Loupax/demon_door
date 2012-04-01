﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using XNAVERGE;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DemonDoor
{
    class GameOverScreen : Screen
    {
        public McGrenderStack mcg;
        internal override void Load()
        {
            Game1 game1 = (Game1)Game1.game;
            //Vector2[] verts = new [] {
            //    new Vector2 { X = -100, Y = 0 },
            //    new Vector2 { X = -70, Y = 0 },
            //    new Vector2 { X = -100, Y = 30 }
            //};

            //Wall _wallTri = new Wall(_world, verts);
            //McgNode rendernode;

            mcg = new McGrenderStack();
            Game1.game.setMcGrender(mcg);

            //mcg.AddLayer("background");

            //McgLayer l = mcg.GetLayer("background");
            ///// this is wrong.
            //Rectangle rectTitle = new Rectangle(0, 0, 320, 240);
            //rendernode = l.AddNode(
            //    new McgNode(game1.im_title, rectTitle, l, 0, 0)
            //);

        }

        internal override string BgBgBg
        {
            get
            {
                return "gameover";
            }
        }

        internal override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if (Game1.game.action.confirm.pressed || Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Space) )
            {
                Game1 game1 = Game1.game as Game1;

                game1.LoadLevel("title");
            }
        }

        private void DrawCentered(SpriteBatch batch, string text, int y, Color color)
        {
            Game1 game1 = (Game1)Game1.game;
            Vector2 size = game1.ft_hud24.MeasureString(text);

            batch.DrawString(game1.ft_hud24, text, new Vector2 { X = (640 - size.X) / 2, Y = y }, color);
        }

        internal override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch, Microsoft.Xna.Framework.GameTime gameTime)
        {
            Game1 game1 = (Game1)Game1.game;
            Vector2 size;

            Color color = Color.White;
            DrawCentered(batch, "You were slain by a flying corpse,", 120, color);
            DrawCentered(batch, "borne aloft on the wings of your", 160, color);
            DrawCentered(batch, "revolving door.", 200, color);

            DrawCentered(batch, "What a shame.", 280, color);

            DrawCentered(batch, "- game over -", 360, Color.Red);
        }

    }
}
