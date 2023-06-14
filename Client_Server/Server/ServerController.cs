/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using NetworkUtil;
using Newtonsoft.Json;
using SnakeGame.Models;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// A class reprensents the controller for Server class to read the setting file.
    /// </summary>
    public class ServerController
    {
        //GameSetting obejct from reading setting file.
        public GameSettings? settings;
        public World theWorld;

        //client's command buffer, used for slow frame rate
        private Dictionary<long, List<Command>> playerCommands;



        //used for random number generator
        private Random random = new Random();
        //used for powerup respawn rate
        private long randomFrame;

        /// <summary>
        /// Constructor
        /// </summary>
        public ServerController()
        {
            theWorld = new World();
            playerCommands = new Dictionary<long, List<Command>>();

        }

        /// <summary>
        /// Reads the settings.xml and initialize related fileds in the GameSetting and theWorld.
        /// </summary>
        public void ReadSetting()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GameSettings));
            using (FileStream fileStream = new FileStream(@"../../../../settings.xml", FileMode.Open))
            {
                TextReader reader = new StreamReader(fileStream);
                settings = (GameSettings)serializer.Deserialize(reader)!;
                World.WorldSize = settings.UniverseSize;
                reader.Close();
            }

        }


        /// <summary>
        /// Initializes the game's mode setting
        /// Updates the world's wall related fields
        /// Generates 5 random powerups to start the game, and start the random power generating mechanism.
        /// </summary>
        public void InitializedGame()
        {
            randomFrame = random.NextInt64(settings!.PowerRespawnRate);
            lock (theWorld)
            {
                //Extra Feature
                if (settings.Mode.Equals("extra"))
                    theWorld.ExtraModeON();

                //Initialize wall-related fields in the world
                for (int i = 0; i < settings!.Walls!.walls!.Count; i++)
                {
                    Wall wall = settings!.Walls!.walls![i];
                    wall.UpdateRect();
                    theWorld.Walls.Add(i, wall);
                }

                //Initialize 5 valid powerups to the world
                if (settings!.PowerMin > 0)
                {
                    for (int i = 0; i < settings!.PowerMin; i++)
                    {
                        theWorld.AddOneValidPowerup();
                    }
                    randomFrame = theWorld.GetNewRandomFrame(settings!.PowerRespawnRate);
                }
               
            }
        }


        /// <summary>
        /// Updates the world.
        /// </summary>
        /// <returns>String represents the message ready to send to the client</returns>
        public string UpdateWorld()
        {
            lock (theWorld)
            {
                //Remove disconnected clients
                foreach (Snake snake in theWorld.disconnectedSnakes)
                {
                    theWorld.Players.Remove(snake.ID);
                }

                //Remove died powerup
                foreach (Powerup powerup in theWorld.diedPowerup)
                {
                    theWorld.Powerups.Remove(powerup.ID);
                }


                HashSet<Snake> disconnectedSnakes = new();
                //updates the snakes
                foreach (Snake snake in theWorld.Players.Values)
                {
                    //lock (snake)
                    //{
                        //ignore disconnected and dead snake
                        if (snake.IsDisConnected && !snake.IsJoin)
                        {
                            //disconnect and not dead: the first frame of disconnection => death effets
                            if (!snake.IsDead)
                            {
                                //Set to Die Version of snake when it's disconnected
                                snake.SetSnakeDeath();
                                theWorld.SnakeDeathToPowerup(snake);
                            }
                            disconnectedSnakes.Add(snake);
                            continue;
                        }

                        //alive: receive command
                        if (!snake.IsDead)
                        {
                            Vector2D direction = HandleMovement(snake);
                            snake.UpdateSnakeMovement(direction);
                            theWorld.CheckCollision(snake);
                        }
                        //not alive or dead:counting frame to start death effects
                        else
                        {
                            snake.UpdateRespawnFrame();
                            if (snake.RespawnFrame == settings!.RespawnRate)
                            {
                                snake.ResetRespawnFrame();
                                theWorld.AddOneValidSnake(snake.ID, snake.name!, settings!.SnakeSpeed, settings.SnakeLength, settings.SnakeGrowth);
                            }
                        }

                    //}
                }

                //every frame checks if we need to update the power
                //And set the frame number for next update
                if (Server.powerupFrame == randomFrame && theWorld.GetNumAlivePower() < settings!.PowerMax)
                {
                    Server.powerupFrame = 0;
                    RespawnPower();
                    randomFrame = theWorld.GetNewRandomFrame(settings!.PowerRespawnRate);
                }

                //Get the objects info to string inside of the world lock
                return SerializeSnakesAndPowerup();

            }
        }

        /// <summary>
        /// Serializes the snake and powerup objects in the world
        /// </summary>
        /// <returns>String reprensents snakes and powerups</returns>
        public string SerializeSnakesAndPowerup()
        {

            StringBuilder sb = new StringBuilder();
            foreach (Powerup powerup in theWorld.Powerups.Values)
            {
                string jsonPowerup = JsonConvert.SerializeObject(powerup);
                sb.Append(jsonPowerup + "\n");
            }

            //Have to send all snakes. Filtering out disconnected snake would cause disconnection freezing.
            foreach (Snake snake in theWorld.Players.Values)
            {
                string jsonSnake = JsonConvert.SerializeObject(snake);
                sb.Append(jsonSnake + "\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Adds a new player to the world,and updates related fileds
        /// </summary>
        /// <param name="id">player id</param>
        /// <param name="name">player's name</param>
        public void HandlerNewPlayer(int id, string name)
        {
            lock (theWorld)
            {
                theWorld.AddOneValidSnake(id, name, settings!.SnakeSpeed, settings.SnakeLength, settings.SnakeGrowth);
            }
        }

        /// <summary>
        /// Adds command to each player's command list per frame by using a string message
        /// </summary>
        /// <param name="id">player's id</param>
        /// <param name="parts">string array message</param>
        public void HandlerCommand(int id, string[] parts)
        {
            foreach (string data in parts)
            {
                //Ignore empty string created by Regex split
                if (data.Length == 0)
                    continue;

                // Ignore the last string that doesn't have "\n" at the end
                if (data[data.Length - 1] != '\n')
                    break;

                //JObject help query
                JToken? tokenMoving;
                JObject obj = JObject.Parse(data);
                tokenMoving = obj["moving"];

                lock (playerCommands)
                {
                    if (tokenMoving is not null)
                    {
                        Command? command = JsonConvert.DeserializeObject<Command>(data);
                        if (command is not null)
                        {

                            if (playerCommands.ContainsKey(id))
                            {
                                playerCommands[id].Add(command);
                            }
                            else
                            {
                                playerCommands.Add(id, new List<Command>());
                                playerCommands[id].Add(command);
                            }
                        }
                    }
                }


            }
        }

        /// <summary>
        /// Applies an orbiturary command from a snake's command list to this snake.
        /// After that, empty the command list waiting for next frame
        /// </summary>
        /// <param name="snake">A snake</param>
        /// <returns>A Vector2D represents the snake's direction for next frame</returns>
        private Vector2D HandleMovement(Snake snake)
        {
            lock (playerCommands)
            {
                if (playerCommands.ContainsKey(snake.ID))
                {
                    List<Command> listCommand = playerCommands[snake.ID];

                    foreach (Command command in listCommand)
                    {
                        Vector2D newDirectionVector = theWorld.GetMovementVector(command.moving!);
                        newDirectionVector.Normalize();
                        snake.Direction!.Normalize();

                        //Validate the direction
                        if (newDirectionVector.IsOppositeCardinalDirection(snake.Direction) ||
                            newDirectionVector.IsOppositeCardinalDirection(snake.previousDirection) && !snake.IsTravelledMoreThanTen())
                        {
                            //Reset the command's list
                            playerCommands[snake.ID] = new List<Command>(); //Reset the command's list
                            //direction not valid, move to the next one.
                            return snake.Direction!;
                        }

                        if (command.moving != "none" && !newDirectionVector.IsOppositeCardinalDirection(snake.Direction) || command == null)
                        {

                            playerCommands[snake.ID] = new List<Command>();
                            //Update new direction
                            return newDirectionVector;
                        }
                    }
                }
                //clear the command list for this snake
                playerCommands[snake.ID] = new List<Command>();
                return snake.Direction!;
            }
        }

        /// <summary>
        /// Respawns power when there is less than power min 
        /// </summary>
        private void RespawnPower()
        {
            if (theWorld.GetNumAlivePower() < settings!.PowerMin)
            {
                theWorld.AddOneValidPowerup();
            }
        }

        /// <summary>
        /// Sets a snake in the world to disconnected state
        /// </summary>
        /// <param name="id">player id</param>
        public void SetDisconnect(long id)
        {
            theWorld.Players[(int)id].SetDisConnectecd();
        }
    }
}
