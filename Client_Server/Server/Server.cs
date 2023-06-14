/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using System;
using System.Collections.Generic;
using NetworkUtil;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SnakeGame.Models;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using System.Xml.Schema;
using System.Net.Mail;

namespace Server
{
    /// <summary>
    /// A class represents the server for Snake Game
    /// </summary>
    public class Server
    {
        private ServerController controller;

        // A map of clients that are connected, each with an ID
        private Dictionary<long, SocketState> clients;

        //frame counts for the program
        public static long frame = 0;

        //frame counts for powerup respawn rate
        public static long powerupFrame = 0;

        static void Main(string[] args)
        {
            Server server = new Server();
            server.controller.ReadSetting();
            server.StartServer();
            server.UpdateAndSend();
        }

        /// <summary>
        /// Updates the the world per frame, and send message to each client.
        /// </summary>
        private void UpdateAndSend()
        {
            Stopwatch watch = new Stopwatch();

            while (true)//server is running
            {
                watch.Restart();
                while (watch.ElapsedMilliseconds < controller.settings!.MSPerFrame) { }

                //Increase the frame by 1
                frame++;
                powerupFrame++;

                //Reset the frame
                if (frame > 60)
                    frame = 0;

                //update the world and serialize the snakes and powers to a string
                string data = controller.UpdateWorld();
                SendToClients(data);
            }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        public Server()
        {
            controller = new ServerController();
            clients = new Dictionary<long, SocketState>();
        }

        /// <summary>
        /// Starts accepting Tcp sockets connections from clients
        /// </summary>
        private void StartServer()
        {
            // This begins an "event loop"
            Networking.StartServer(NewClientConnected, 11000);

            //Initialize the game
            controller.InitializedGame();

            Console.WriteLine("Server is running");
        }

        /// <summary>
        /// Sends messages to healthy clients, and removes disconnected clients from server.
        /// </summary>
        /// <param name="data">string represents the message</param>
        private void SendToClients(string data)
        {
            HashSet<long> disconnectedClients = new HashSet<long>();
            lock (clients)
            {
                foreach (SocketState client in clients.Values)
                {
                    if (!Networking.Send(client.TheSocket, data))
                        disconnectedClients.Add(client.ID);
                }
            }
            foreach (long id in disconnectedClients)
            {
                RemoveClient(id);
            }
        }


        /// <summary>
        /// Method to be invoked by the networking library
        /// when a new client connects
        /// </summary>
        /// <param name="state">The SocketState representing the new client</param>
        private void NewClientConnected(SocketState state)
        {
            if (state.ErrorOccurred)
            { 
                return;
            }

            Console.WriteLine("client " + state.ID + " connected");

            //Start receiving the name
            state.OnNetworkAction = ReceiveHandShake;
            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a network action occurs
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveHandShake(SocketState state)
        {
            // Remove disconnected client and set their snake to disconnected state
            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                HandleError(state.ID);
                return;
            }

            //Send startup info: receive name , and send walls
            ProcessHandShake(state);

            //Change Action, ask for data
            state.OnNetworkAction = ReceiveCommand;
            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }

        ///<summary>
        /// Method to be invoked by the networking library when 
        /// command message is available
        /// </summary>
        /// <param name="state">Socketstate</param>
        private void ReceiveCommand(SocketState state)
        {
            // Remove disconnected client and set their snake to disconnected state
            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                HandleError(state.ID);
                return;
            }

            //Process the command
            ProcessCommand(state);

            //Ask for data (infinite event loop), Calling ReceiveCommand infinitely
            Networking.GetData(state);
        }

        /// <summary>
        /// Deserializes and sends handshake messages. And Adds a new player to the world.
        /// </summary>
        /// <param name="state">A SocketState</param>
        private void ProcessHandShake(SocketState state)
        {
            //totalData is Json string 
            string totalData = state.GetData();

            //split the message by new line 
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            foreach (string data in parts)
            {
                //Ignore empty string created by Regex split
                if (data.Length == 0)
                    continue;

                // Ignore the last string that doesn't have "\n" at the end
                if (data[data.Length - 1] != '\n')
                    break;

                int id = (int)state.ID;

                string name = data.Replace("\n", "");

                //Process the name

                if (name is not null)
                {
                    // Send startup info
                    SendHandshake(state);

                    // Saving SocketState
                    lock (clients)
                    {
                        clients[id] = state;
                    }

                    //Let's controller handle new player(create new snake)
                    controller.HandlerNewPlayer(id, name);
                }

                //Remove the processed data from the socket state
                state.RemoveData(0, data.Length);


            }
        }

        /// <summary>
        ///Derializes command message from the client, and let controller handle the message
        /// </summary>
        /// <param name="sender">The SocketState that represents the client</param>
        private void ProcessCommand(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");
            int id = (int)state.ID;

            // Let's the controller handler the command from players
            controller.HandlerCommand(id, parts);


            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, totalData.Length);

        }


        /// <summary>
        /// Removes a client from the clients dictionary
        /// </summary>
        /// <param name="id">The ID of the client</param>
        private void HandleError(long id)
        {
            controller.SetDisconnect(id);
        }


        /// <summary>
        /// Serializes walls and send handshake string to the clients. 
        /// </summary>
        /// <param name="state">SocketState</param>
        private void SendHandshake(SocketState state)
        {
            StringBuilder data = new();
            data.Append(state.ID + "\n" + controller.settings!.UniverseSize + "\n");

            //Send the walls and update the world
            foreach (Wall wall in controller.settings!.Walls!.walls!)
            {
                data.Append( JsonConvert.SerializeObject(wall) +"\n");
            }

            Networking.Send(state.TheSocket, data.ToString());
        }

        /// <summary>
        /// Removes a client from the clients dictionary
        /// </summary>
        /// <param name="id">The ID of the client</param>
        private void RemoveClient(long id)
        {
            Console.WriteLine("Client " + id + " disconnected");
            lock (clients)
            {
                clients.Remove(id);
            }
        }


    }
}