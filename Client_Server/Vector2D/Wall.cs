/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using Newtonsoft.Json;
using SnakeGame.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SnakeGame.Models
{
    /// <summary>
    /// A class represents a wall.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Wall
    {
        [JsonProperty("wall")]
        [XmlElement("ID")]
        public int ID { get; set; }

        [JsonProperty("p1")]
        [XmlElement("p1")]
        public Vector2D Position1 { get; set; }
        [JsonProperty("p2")]
        [XmlElement("p2")]
        public Vector2D Position2 { get; set; }

        //A RectangleF of a wall
        public RectangleF Rect;

        //WorldSize of a wall unit(50x50)
        private int size = 50;

        //Property to check the wall's direction
        public bool IsHorizontal
        {
            get { return Position1.GetX() != Position2.GetX(); }
        }

        /// <summary>
        /// Defualt constructor
        /// </summary>
        public Wall()
        {
            Position1 = new();
            Position2 = new();
            Rect = new();
            //Console.WriteLine(Rect.ToVector4());
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="id">unique ID for each wall</param>
        /// <param name="x1">x cordinate for one end of the wall</param>
        /// <param name="y1">y cordinate for one end of the wall</param>
        /// <param name="x2">x cordinate for the other end of the wall</param>
        /// <param name="y2">y cordinate for the other end of the wall</param>
        public Wall(int id, int x1, int y1, int x2, int y2)
        {
            ID = id;
            Position1 = new Vector2D(x1, y1);
            Position2 = new Vector2D(x2, y2);
            Rect = World.VectorToRect(Position1, Position2, size);
        }

        /// <summary>
        /// Gets the width of a wall block
        /// </summary>
        /// <returns></returns>
        public double GetWidth()
        {
            return Math.Abs(Position1.GetX() - Position2.GetX()) + 50;
        }

        /// <summary>
        /// Gets the height of a wall block
        /// </summary>
        /// <returns></returns>
        public double GetHeight()
        {
            return Math.Abs(Position1.GetY() - Position2.GetY()) + 50;
        }

        /// <summary>
        /// Updates the RectangleF represents the wall
        /// </summary>
        public void UpdateRect()
        {
            Rect = World.VectorToRect(Position1, Position2, size);
        }

    }
}
