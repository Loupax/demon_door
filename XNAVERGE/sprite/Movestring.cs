﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNAVERGE {

    // Represents an instance of an entity movestring, including instance-specific state information.
    public class Movestring {
        public const bool DEFAULT_TO_TILE_MOVEMENT = true; // assume tile movement if a movestring does not specify
        public const int NO_NUMBER = Int32.MinValue; // indicates no parameter is associated with a command
        public const int NEVER_TIMEOUT = -1;

        // constants for the weird custom format this uses
        public const int FACECODE_UP = 1;
        public const int FACECODE_DOWN = 0;
        public const int FACECODE_LEFT = 2;
        public const int FACECODE_RIGHT = 3;        

        public static Regex regex = new Regex(
                @"([ulrdxyptzwfb])\s*(\d*)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);


        public MovestringCommand[] commands;
        public int[] parameters;

        // State variables
        public bool tile_movement; // specifies whether or not directional commands are in pixels or tiles
        public bool done;        
        public int step;
        public event Action OnDone; // TODO: remove this once it's no longer needed
        public event MovestringEndingDelegate on_done; // called when the movestring completes or times out on an obstruction
        public int timeout; // The movestring times out if the entity spends this long pushing something (measured in engine ticks)

        public int movement_left; // distance left to move for the current move command, measured in hundredths of pixels. 0 if not at a move command.        
        protected int wait_time; // time left to wait, in hundredths of ticks. 0 if not waiting.

        public void stop() { stop(true); }
        public void stop(bool treat_as_aborted) {
            done = true;

            if (OnDone != null) VERGEGame.game.action_queue.Enqueue(OnDone);
            if (on_done != null) VERGEGame.game.action_queue.Enqueue(() => { on_done(ent, treat_as_aborted); });
        }

        Entity ent = null;

        public Movestring(String movestring) : this(movestring, null, null, NEVER_TIMEOUT) { }
        public Movestring(String movestring, Entity e) : this(movestring, null, e, NEVER_TIMEOUT) { }
        public Movestring(String movestring, MovestringEndingDelegate callback, Entity e) : 
            this(movestring, callback, e, NEVER_TIMEOUT) {}
        public Movestring(String movestring, MovestringEndingDelegate callback, Entity e, int timeout) {
            ent = e;
            if (callback != null) on_done += callback;
            this.timeout = timeout;

            MatchCollection matches = regex.Matches(movestring);
            GroupCollection groups;
            Queue<MovestringCommand> command_queue = new Queue<MovestringCommand>();
            Queue<int> param_queue = new Queue<int>();

            restart();

            if (String.IsNullOrEmpty(movestring)) {
                commands = new MovestringCommand[1];
                parameters = new int[1];
                commands[0] = MovestringCommand.Stop;
                parameters[0] = NO_NUMBER;
                stop(false);
                return;
            }            

            MovestringCommand command;
            int parameter;
            bool open_ended = true;
            bool time_consuming = false; 

            foreach (Match match in matches) {
                groups = match.Groups;
                parameter = NO_NUMBER;
                if (!String.IsNullOrEmpty(groups[2].Value)) parameter = Int32.Parse(groups[2].Value);
                param_queue.Enqueue(parameter);
                
                // WHO HERE LIKES HUGE SWITCHES? YEAH!!
                switch (groups[1].Value) {
                    case "B": // loop back to start. 
                        // This normally takes no number parameter, but unlike VERGE, you can include one, in which case
                        // it will loop that many times, then stop. 
                        if (!time_consuming) {
                            command = MovestringCommand.Stop; // sanity check against inescapable processing loop
                            parameter = NO_NUMBER;
                        }
                        else command = MovestringCommand.Loop;
                        open_ended = false;
                        break;
                    case "T": // switch to pixel coordinates
                        command = MovestringCommand.TileMode;
                        break;
                    case "P": // switch to tile coordinates
                        command = MovestringCommand.PixelMode;
                        break;
                    case "Z": // frame switch (may or may not have a number parameter)
                        // This locks the entity into the specified frame, suppressing animation during movement. In VERGE, setting it to 0
                        // restored the entity to normal animation, but this is a problem if you want to lock it at frame 0. Thus, I'm changing
                        // the rule: Z0 locks the entity at frame 0, and Z with no number after it restores normal animation.
                        // Be warned that this may mess up the occasional .MAP-embedded movestring.
                        command = MovestringCommand.Frame;                        
                        break;
                    case "F": // face (not necessary, since a distance-0 move accomplishes the same thing)
                        command = MovestringCommand.Face;                        
                        break;
                    case "U": // move up
                        command = MovestringCommand.Up;
                        time_consuming = true;
                        break;
                    case "D": // move down
                        command = MovestringCommand.Down;
                        time_consuming = true;
                        break;
                    case "L": // move left
                        command = MovestringCommand.Left;
                        time_consuming = true;
                        break;
                    case "R": // move right
                        command = MovestringCommand.Right;
                        time_consuming = true;
                        break;
                    case "W": // wait
                        command = MovestringCommand.Wait;
                        time_consuming = true;
                        break;
                    case "X": // walk straight to specific x
                        command = MovestringCommand.ToX;
                        time_consuming = true;
                        break;
                    case "Y": // walk straight to specific y
                        command = MovestringCommand.ToY;
                        time_consuming = true;
                        break;
                    default: throw new MalformedMovestringException(movestring);                        
                }
                command_queue.Enqueue(command);
            }

            if (open_ended) {
                command_queue.Enqueue(MovestringCommand.Stop);
                param_queue.Enqueue(Movestring.NO_NUMBER);
            }

            commands = new MovestringCommand[command_queue.Count];
            parameters = new int[commands.Length];

            for (int i = 0; i < commands.Length; i++) {
                commands[i] = command_queue.Dequeue();
                parameters[i] = param_queue.Dequeue();
            }
        }        

        // Restarts the movestring from the beginning, but does NOT reset the loop counter if the movestring us a finite loop.
        public void restart() {
            step = 0;
            done = false;
            tile_movement = DEFAULT_TO_TILE_MOVEMENT;
            wait_time = 0;
            movement_left = 0;
        }

        // Abort and execute the movestring callback immediately. This generally means that the entity has timed out
        // pushing against an obstruction. 
        // (No effect if there is no movestring callback)
        public void do_timeout() {
            stop(true);
        }

        // Processes the movestring, returning when it gets to a blocking point or requires outside handling. It takes as an
        // argument the (speed-adjusted) time elapsed in hundredths of ticks, and returns how much of that is "left over"
        // after processing. If it returns 0, it means it's hit a blocking point.
        // It takes the entity's speed as an argument. This works differently depending on the current command.
        //    * If the current command is a move (UDLRXY), face (F), or frame (Z) command, advances by one step and returns.
        //    * If the current command is a wait (W) and the wait time has not yet passed, returns 0.
        //    * If the current command is a wait (W) and the wait time is up, decrements the time elapsed by however much wait
        //    * time was left, then processes the next command.
        //    * If the current command is a stop, sets done to true and returns 0.
        //    * If the current command is a pixel/tile mode switch, applies that internally and then processes the next command.
        //    * If the current command is a loop, loops back to the first command and processes it. If the loop is finite, its 
        //      counter is decremented, and if this reduces it to 0, the Loop command is replaced with Stop.
        // In other words it won't return until it reaches a move, face, frame, or stop command, OR a wait whose time hasn't yet elapsed.
        public int ready(int elapsed) {
            if (done) return 0;
            while (true) {
                switch (commands[step]) {
                    case MovestringCommand.Wait:
                        if (wait_time == 0) wait_time = parameters[step] * 100;
                        wait_time -= elapsed;
                        if (wait_time > 0) return 0; // still waiting
                        else {
                            elapsed = -wait_time;
                            wait_time = 0;
                            step++;
                            if (commands[step] == MovestringCommand.Wait) // if for some reason you chained two waits, WHY WOULD YOU DO THAT but anyhow it'll work
                                wait_time = parameters[step];
                        }
                        break;
                    case MovestringCommand.Stop:
                        stop(false);
                        return 0; // being stopped absorbs all time spent, like an infinite wait
                    case MovestringCommand.Loop:
                        if (parameters[step] != NO_NUMBER) { // finite loop
                            parameters[step]--;
                            if (parameters[step] <= 0) // this will be the last iteration
                                commands[step] = MovestringCommand.Stop;                           
                        }
                        restart();
                        break;
                    case MovestringCommand.PixelMode:
                        tile_movement = false;
                        step++;
                        break;
                    case MovestringCommand.TileMode:
                        tile_movement = true;
                        step++;
                        break;
                    default: return elapsed; // case Up, Down, Left, Right, Frame, Face, ToX, ToY
                }   
            }
        }

    }

    // An enumeration of movestring actions. "Stop" is not actually used in movestrings -- it indicates a non-looping end and is inserted by the loader.
    public enum MovestringCommand { Up, Down, Left, Right, Wait, Frame, Face, Loop, PixelMode, TileMode, ToX, ToY, Stop }

    public class MalformedMovestringException : Exception {
        public MalformedMovestringException(String movestring) : base("\"" + movestring + "\" is not a valid movestring. Each term must be one of U, D, L, R, W, Z, F, or B followed by a nonnegative number, or one of Z, B, P, or T by itself. For more information, consult http://verge-rpg.com/docs/the-verge-3-manual/entity-functions/entitymove/.") { }
    }

    // An enumeration of wander styles. The first, "scripted", covers both the "static" and "scripted" modes in normal VERGE and denotes an entity
    // that does not wander at random.
    public enum WanderMode { Scripted, Zone, Rectangle };

    // A state variable for VERGE-style entity wandering. 
    // TODO: Eventually, Entity.movestring should be moved inside this.
    public class WanderState {
        public WanderMode mode;
        public Entity ent;
        public Rectangle rect; // If mode = Rectangle, this defines the wanderable tile region 
        public int delay; // if mode = Rectangle or Zone, this is the wait time between wander steps.

        public WanderState(Entity entity) { 
            ent = entity;
            mode = WanderMode.Scripted;
            delay = 0;
            rect = default(Rectangle);
        }

        // Returns true if the given tile is within the entity's wander rectangle (if in Rectangle mode)
        // or is of the same zone (if in Zone mode). Otherwise, returns false. Note that this doesn't
        // check that there's actually a path from the entity's current position to the given tile,
        // or that it's unobstructed.
        public bool can_wander_to(int tx, int ty) {
            Point pt;
            VERGEMap map = VERGEGame.game.map;
            if (tx < 0 || ty < 0 || tx >= map.width || ty >= map.height) return false; // can't leave map
            if (mode == WanderMode.Rectangle &&
                tx >= rect.Left && tx < rect.Right && ty >= rect.Top && ty < rect.Bottom) return true;
            else if (mode == WanderMode.Zone) {
                pt = ent.hitbox.Center;
                pt.X /= map.tileset.tilesize;
                pt.Y /= map.tileset.tilesize;
                if (pt.X < 0 || pt.Y < 0 || pt.X >= map.width || pt.Y >= map.height) return true; // can't leave map
                return (map.zone_layer.data[tx][ty] ==
                        map.zone_layer.data[pt.X][pt.Y]);
            }
            return false;
        }
    }
}
