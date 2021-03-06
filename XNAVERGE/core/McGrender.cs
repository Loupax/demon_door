﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using XNAVERGE.sprite;

namespace XNAVERGE {


    public delegate void BasicMcgNodeDelegate( McgNode curNode );

    public class McgNode {
        McgLayer layer;

        float start_x, start_y;
        float final_x, final_y;
        float cur_x, cur_y;
        float? tick_x, tick_y;
        int? delay, final_time;
        Boolean isMoving;
        public BasicMcgNodeDelegate OnStop = null;
        public RenderDelegate OnDraw = null;
        public UpdateDelegate OnUpdate = null; 

        public bool DEBUG = false;

        Texture2D image = null;
        Rectangle im_bounds;

        public McgNode( RenderDelegate act, McgLayer l, int start_x, int start_y) : this(act, l, start_x, start_y, null, null, null ) { }
        public McgNode( RenderDelegate act, McgLayer l, int start_x, int start_y, int? end_x, int? end_y, int? delay) {
            OnDraw = act;
            _Node( l, start_x, start_y, end_x, end_y, delay );
        }

        public McgNode( Texture2D im, Rectangle? bounds, McgLayer l, int start_x, int start_y ) : this(im, bounds, l, start_x, start_y, null, null, null ) { }
        public McgNode( Texture2D im, Rectangle? bounds, McgLayer l, int start_x, int start_y, int? end_x, int? end_y, int? delay ) {
            image = im;

            if( bounds == null ) {
                im_bounds = new Rectangle( start_x, start_y, im.Width, im.Height );
            } else {
                im_bounds = bounds.Value;
            }
            
            _Node( l, start_x, start_y, end_x, end_y, delay );
        }

        IDrawableThing _idt = null;
        public McgNode( IDrawableThing d, McgLayer l, int start_x, int start_y ) {
            _idt = d;
            _Node( l, start_x, start_y, start_x, start_y, 0 );
        }

        private void _Node( McgLayer l, int start_x, int start_y ) { _Node(l, start_x, start_y); }
        private void _Node( McgLayer l, int start_x, int start_y, int? end_x, int? end_y, int? delay) {
            layer = l;

            this.start_x = start_x;
            this.start_y = start_y;

            if( end_x == null ) {
                cur_x = final_x = start_x;
                cur_y = final_y = start_y;
                tick_y = tick_x = delay = final_time = null;
                isMoving = false;
            } else {
                /*
                cur_x = start_x;
                cur_y = start_y;
                final_x = (float)end_x;
                final_y = (float)end_y;
                this.delay = delay;
                tick_x = ( final_x - cur_x ) / (float)delay;
                tick_y = ( final_y - cur_y ) / (float)delay;
                final_time = layer.stack.systime + delay;
                isMoving = true;
                */

                SetNewMovement( start_x, start_y, end_x.Value, end_y.Value, delay.Value );
            }
        }

        public void SetNewMovement( int start_x, int start_y, int end_x, int end_y, int delay ) {
            cur_x = start_x;
            cur_y = start_y;
            final_x = (float)end_x;
            final_y = (float)end_y;
            this.delay = delay;
            tick_x = ( final_x - cur_x ) / (float)delay;
            tick_y = ( final_y - cur_y ) / (float)delay;
            final_time = layer.stack.systime + delay;
            isMoving = true;
        }
 
        public override string ToString() {
            return  "Node layer " + layer + 
                    " cur: (" + cur_x + "," + cur_y + 
                    ") final: (" + final_x + "," + final_y + 
                    ") delay: " + delay + 
                    " tick: (" + tick_x + "," + tick_y + 
                    ") final_time: (" + final_time + 
                    ") curtime: " + layer.stack.systime;   
        }

        public Boolean IsMoving() {
            return isMoving;
        }

        public void Update( GameTime gt ) {

            if( _idt != null && _idt is IBrainyThing ) {
                IBrainyThing brainy = _idt as IBrainyThing;

                brainy.ProcessBehavior( gt );
            } else {
                if( DEBUG ) Console.WriteLine( "ticksSinceLastUpdate: " + gt.ElapsedGameTime.Milliseconds );
                if( DEBUG ) Console.WriteLine( this );

                if( isMoving ) {

                    if( DEBUG ) Console.WriteLine( "IS MOVING" );
                    if( gt.ElapsedGameTime.Milliseconds > 0 ) {
                        this.cur_x += ( (float)tick_x * (float)gt.ElapsedGameTime.Milliseconds );
                        this.cur_y += ( (float)tick_y * (float)gt.ElapsedGameTime.Milliseconds );

                        if( DEBUG ) Console.WriteLine( this );
                        if( layer.stack.systime >= final_time ) {
                            if( DEBUG ) Console.WriteLine( "STOPPING" );
                            isMoving = false;
                            this.cur_x = this.final_x;
                            this.cur_y = this.final_y;

                            //this.Reverse();

                            if( OnStop != null ) {
                                OnStop(this);
                            }
                        }
                    }

                    if( image != null ) {
                        this.im_bounds = new Rectangle( (int)cur_x, (int)cur_y, image.Width, image.Height );
                    }
                }
            }
        }

        public void Draw() {
            if( _idt != null ) {
                _idt.GetDrawDelegate()( _idt.GetX(), _idt.GetY() );
            } else if( image != null ) {
                layer.stack.spritebatch.Draw( image, im_bounds, Color.White );
            } else if( OnDraw != null ) {
                OnDraw( (int)this.cur_x, (int)this.cur_y );
            }
        }

        public void Reverse() {
            float temp_x, temp_y;

            if( !(tick_x != 0 || tick_y != 0) ) {
                return;
            }

            //reverse the chunks
            this.tick_x = -this.tick_x;
            this.tick_y = -this.tick_y;

            temp_x = final_x;
            temp_y = final_y;

            final_x = start_x;
            final_y = start_y;

            start_x = temp_x;
            start_y = temp_y;

            isMoving = true;
            final_time = layer.stack.systime + delay;
        }
    }



    public class McgLayer {

        public McGrenderStack stack;
        public Boolean isRendering;
        public List<McgNode> nodes;
        public string name;

        public McgLayer( McGrenderStack s, string n ) {
            stack = s;
            nodes = new List<McgNode>();
            isRendering = true;
            name = n;
        }

        public McgNode AddNode( McgNode n ) {
            nodes.Add( n );
            return n;
        }

        public void Update( GameTime gt ) {
            for( int i = 0; i < nodes.Count; i++ ) {
                nodes[i].Update( gt );
            }
        }

        public void Draw() {
            for( int i = 0; i < nodes.Count; i++ ) {
                nodes[i].Draw();
            }
        }
    }

    public class McGrenderStack {    
        public List<McgLayer> layers;
        public SpriteBatch spritebatch;
        public int systime;
        public int prev_systime;

        public void setSpritebatch( SpriteBatch sb ) {
            spritebatch = sb;
        }

        public void setSystime( long st ) {
            prev_systime = systime;
            
            systime = (int)st;
        }

        GameTime _gt = null;
        public void setGameTime( GameTime gt ) {
            _gt = gt;
        }

        public McGrenderStack() {
            layers = new List<McgLayer>();
        }

        public McgLayer AddLayer( string name ) {
            McgLayer l = new McgLayer(this, name);
            layers.Add( l );
            return l;
        }

        public McgLayer GetLayer( string name ) {
            for( int i = 0; i < layers.Count; i++ ) {
                if( layers[i].name == name ) {
                    return layers[i];
                }
            }

            return null;
        }

        public void Update() {
            Update( _gt );
        }

        public void Update( GameTime gt ) {
            for( int i = 0; i < layers.Count; i++ ) {
                layers[i].Update( gt );
            }
        }

        public void Draw() {
            //Update( systime - prev_systime );

            spritebatch.Begin();
            for( int i = 0; i < layers.Count; i++ ) {
                layers[i].Draw();
            }

            spritebatch.End();
        }
    }
}
