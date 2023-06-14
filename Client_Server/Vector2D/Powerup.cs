/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGame.Models
{
    /// <summary>
    /// Powerup Class represents food in the world.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Powerup
    {
        [JsonProperty("power")]
        public int ID { get; private set; }
        [JsonProperty("loc")]
        public Vector2D? Position { get; private set; }
        [JsonProperty("died")]
        public bool Died { get; set; } = false;

        //A RectangleF represnets a powerup
        public RectangleF Rect;
        public int size { get; private set; } = 10;


        /// <summary>
        /// Default Constructor
        /// </summary>
        public Powerup()
        {
            Rect = new();
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="id">Unique ID for each powerup</param>
        /// <param name="x">x cordinate of powerup</param>
        /// <param name="y">y cordinate of powerup</param>
        public Powerup(int id, Vector2D location)
        {
            ID = id;
            Position = new Vector2D(location);
            Rect = World.VectorToRect(Position!, size);
        }

    }
}
