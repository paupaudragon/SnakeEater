/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */

namespace SnakeGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;
    using SnakeGame.Models;
    using NetworkUtil;
    using System.Text.RegularExpressions;
    using System.Text.Json.Nodes;
    using Newtonsoft.Json;
    using System.Xml.Linq;
    using Newtonsoft.Json.Linq;
    using System.Diagnostics;
    using System.Reflection.Metadata.Ecma335;


    /// <summary>
    /// GameController from Snake Game that gets information from the server 
    /// and updates the models and then inform the View. 
    /// This class uses the  Networking libarary built in PS7.
    /// </summary>
    public class GameController
    {

        private World theWorld;//Game world that contains all the objects
        private SocketState? theServer;//SocketState representing a connection
        public string? playerName;//Player's name for this client
        public bool startDeath = false;

        //inform the view of a successful connection
        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;

        //inform the view when they can use command
        public delegate void CommandEnabler();
        public event CommandEnabler? EnableCommand;

        //inform the view of server's update
        public delegate void GameUpdateHandler();
        public event GameUpdateHandler? UpdateArrived;

        //inform the view if there is an error
        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;

        /// <summary>
        /// Constructor
        /// </summary>
        public GameController()
        {
            //Initialize an empty world.
            theWorld = new World();
        }

        /// <summary>
        /// Sends commands to the server
        /// </summary>
        /// <param name="message">a command message</param>
        public void SendCommand(string message)
        {
            Networking.Send(theServer!.TheSocket, message);
        }

        /// <summary>
        /// Returns the Wold object contained in this class
        /// </summary>
        /// <returns>a world</returns>
        public World GetWorld()
        {
            return theWorld;

        }

        /// <summary>
        /// Attempts to connect to the server and initializes this client's player name.
        /// </summary>
        /// <param name="addr">server's ip address client attempt to connect to</param>
        /// <param name="name">this client's player name given by the user</param>
        public void Connect(string addr, string name)
        {
            playerName = name;
            Networking.ConnectToServer(OnConnect, addr, 11000);
        }

        /// <summary>
        /// Method to be invoked by the networking library when a connection is made
        /// </summary>
        /// <param name="state">Socketstate</param>
        private void OnConnect(SocketState state)
        {
            //Inform the view of any connection error
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Error connecting to server");
                return;
            }

            //Send the valid player name to the server
            Networking.Send(state.TheSocket, playerName + "\n");

            // inform the view connection is successful 
            Connected?.Invoke();
            theServer = state;

            //start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveHandShake;
            Networking.GetData(state);
        }

        ///<summary>
        /// Method to be invoked by the networking library when 
        /// data is available
        /// </summary>
        /// <param name="state">Socketstate</param>
        private void ReceiveHandShake(SocketState state)
        {
            //Inform the view of any connection error
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Lost connection to server.");
                return;
            }

            //Update the model's player id, world size, and walls
            //OnNetwork DELEGATE is reassigned in this method when necessary
            ProcessHandShakeMessage(state);

            //Continue the event loop to receive more message if any
            Networking.GetData(state);
        }


        ///<summary>
        /// Method to be invoked by the networking library when 
        /// data is available
        /// </summary>
        /// <param name="state">Socketstate</param>
        private void ReceiveMessagesPostHandShake(SocketState state)
        {
            //Inform the view of any connection error
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Lost connection to server.");
                return;
            }

            //Update the model using messages from the server
            ProcessPostHandShakeMessages(state);

            //Delegate is still ReceiveMessagesPostHandShake, no need to reassign
            Networking.GetData(state);
        }

        /// <summary>
        /// Deserilizes the message from server to world objects and update the world.
        /// </summary>
        /// <param name="state">A SocketState</param>
        private void ProcessHandShakeMessage(SocketState state)
        {

            //totalData is Json string 
            string totalData = state.GetData();

            //split the message by new line 
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            foreach (string data in parts)
            {
                //System.Diagnostics.Debug.WriteLine(data);

                //Ignore empty string created by Regex split
                if (data.Length == 0)
                    continue;

                // Ignore the last string that doesn't have "\n" at the end
                if (data[data.Length - 1] != '\n')
                    break;

                lock (theWorld)
                {
                    //Get handShake messages
                    if (theWorld.PlayerID == -1)
                    {
                        try
                        {
                            theWorld.PlayerID = int.Parse(data);
                        }
                        catch
                        {
                            continue;
                        }

                    }

                    else if (World.WorldSize == -1)
                    {
                        try
                        {
                            World.WorldSize = int.Parse(data);
                        }
                        catch
                        {
                            continue;
                        }

                    }
                    else
                    {
                        //Deserialize to wall 
                        JToken? tokenForWall;
                        JObject obj = JObject.Parse(data);
                        tokenForWall = obj["wall"];
                        if (tokenForWall is not null)
                            DeserializeWall(data);

                        //walls data are finished
                        else
                        {
                            EnableCommand?.Invoke();
                            state.OnNetworkAction = ReceiveMessagesPostHandShake;//change the delegate to receive post handshake data
                        }
                    }
                }

                //Remove the processed data from the socket state
                state.RemoveData(0, data.Length);
            }

            //Inform the view update arrives
            UpdateArrived?.Invoke();

        }

        /// <summary>
        /// Deserilizes the message from server to world objects and update the world.
        /// </summary>
        /// <param name="state">A SocketState</param>
        private void ProcessPostHandShakeMessages(SocketState state)
        {

            //totalData is Json string 
            string totalData = state.GetData();

            //split the message by new line 
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            foreach (string data in parts)
            {
                //System.Diagnostics.Debug.WriteLine(data);

                //Ignore empty string created by Regex split
                if (data.Length == 0)
                    continue;

                // Ignore the last string that doesn't have "\n" at the end
                if (data[data.Length - 1] != '\n')
                    break;

                lock (theWorld)
                {
                    JToken? tokenForPower, tokenForSnake;
                    JObject obj = JObject.Parse(data);

                    //Deserialize to snake
                    tokenForSnake = obj["snake"];
                    if (tokenForSnake is not null)
                        DeserializeSnake(data);

                    //Deserialize to powerup
                    tokenForPower = obj["power"];
                    if (tokenForPower is not null)
                        DeserializePower(data);

                }

                //Remove the processed data from the socket state
                state.RemoveData(0, data.Length);

            }

            //Inform the view update arrives
            UpdateArrived?.Invoke();

        }

        /// <summary>
        /// Derializes Snake Json string and updates the Model
        /// </summary>
        /// <param name="jsonString">a Json string represents a snake object</param>
        private void DeserializeSnake(string jsonString)
        {
            //System.Diagnostics.Debug.WriteLine("Snake: " + jsonString);

            Snake? snake = JsonConvert.DeserializeObject<Snake>(jsonString);

            //if the snake already in the world
            if (theWorld.Players.ContainsKey(snake!.ID))
            {
                if (snake.IsDisConnected)
                {
                    theWorld.Players.Remove(snake.ID);
                }
                //If the snake is moved, useful for low fps
                else if (snake!.IsMoving(theWorld.Players[snake.ID].Position))
                {
                    theWorld.Players.Remove(snake.ID);
                    theWorld.Players.Add(snake.ID, snake);
                }
            }
            //The snake is new 
            else
            {
                theWorld.Players.Add(snake!.ID, snake);
            }
        }

        /// <summary>
        /// Desirializes a Json string to a wall and updates the Model
        /// </summary>
        /// <param name="jsonString">a Json string represents a wall object</param>
        private void DeserializeWall(string jsonString)
        {
            //System.Diagnostics.Debug.WriteLine(jsonString);

            Wall? wall = JsonConvert.DeserializeObject<Wall>(jsonString);
            if (wall is not null)
            {
                if (theWorld.Walls.ContainsKey(wall!.ID))
                    theWorld.Walls.Remove(wall.ID);
                theWorld.Walls.Add(wall.ID, wall);
            }


        }

        /// <summary>
        ///  Desirializes a Json string to a powerup and updates the Model
        /// </summary>
        /// <param name="jsonString">a Json string represents a powerup object</param>
        private void DeserializePower(string jsonString)
        {
            Powerup? power = JsonConvert.DeserializeObject<Powerup>(jsonString);
            //// Extra Feature PS9
            //if (power!.IsSuper)
            //{
            //    if (!theWorld.SuperPowerups.ContainsKey(power!.ID))
            //        theWorld.SuperPowerups.Add(power!.ID, power);
            //}
            //else
            //{
            //    if (!theWorld.Powerups.ContainsKey(power!.ID))
            //        theWorld.Powerups.Add(power!.ID, power);
            //}
            

            //if (power!.Died)
            //{   // Extra Feature PS9
            //    if (power!.IsSuper) theWorld.SuperPowerups.Remove(power.ID, out power);
            //    else theWorld.Powerups.Remove(power.ID, out power);

            //}

            if (!theWorld.Powerups.ContainsKey(power!.ID))
                theWorld.Powerups.Add(power!.ID, power);
            if (power!.Died)
            {    
                theWorld.Powerups.Remove(power.ID, out power);
            }
        }

    }
}