/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeGame.Models;
using System.Xml.Serialization;

namespace Server
{
    /// <summary>
    /// Class represents a GameSettings 
    /// </summary>
    [XmlRoot("GameSettings")]
    public class GameSettings
    {
        [XmlElement("FramesPerShot")]
        public int FramesPerShot { get; set; }
        [XmlElement("MSPerFrame")]
        public int MSPerFrame { get; set; }
        [XmlElement("RespawnRate")]
        public int RespawnRate { get; set; }
        [XmlElement("UniverseSize")]
        public int UniverseSize { get; set; }

        /****************Added for extra features******/
        [XmlElement("PowerRespawnRate")]
        public int PowerRespawnRate { get; set; } = 200;
        [XmlElement("PowerMax")]
        public int PowerMax { get; set; } = 20;
        [XmlElement("PowerMin")]
        public int PowerMin { get; set; } = 5;
        [XmlElement("SnakeGrowth")]
        public int SnakeGrowth { get; set; } = 12;
        [XmlElement("SnakeLength")]
        public int SnakeLength { get; set; } = 120;
        [XmlElement("SnakeSpeed")]
        public int SnakeSpeed { get; set; } = 3; //Suggest don't set the speed too high
        [XmlElement("Mode")]
        public string Mode { get; set; } = "basic";
        /*******************************************/


        [JsonProperty("Walls")]
        [XmlElement("Walls")]
        public WallList? Walls { get; set; }

        /// <summary>
        /// A class represents a WallList
        /// This is used for XML deserialization
        /// </summary>
        public class WallList
        {
            [XmlElement("Wall")]
            public List<Wall>? walls;

            /// <summary>
            /// Default constructor
            /// </summary>
            public WallList() { }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GameSettings() { }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="f"></param>
        /// <param name="m"></param>
        /// <param name="r"></param>
        /// <param name="u"></param>
        /// <param name="wallList"></param>
        public GameSettings(int f, int m, int r, int u, WallList wallList)

        {
            FramesPerShot = f;
            MSPerFrame = m;
            RespawnRate = r;
            UniverseSize = u;
            Walls = wallList;

        }
    }
}
