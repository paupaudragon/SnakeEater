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
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGame.Models
{
    /// <summary>
    /// A class represents the wolrd of the game
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class World
    {
        public Dictionary<int, Snake> Players;
        public Dictionary<int, Powerup> Powerups;
        public Dictionary<int, Wall> Walls;
        public int powerupCount = 0;
        public HashSet<Powerup> diedPowerup;
        public HashSet<Snake> disconnectedSnakes;

        public int PlayerID = -1;
        public bool IsExtraMode = false;
        public static int WorldSize = -1;
        public static int powerupSize = 10;
        public static int snakeSize = 10;
        public static int wallSize = 50;

        //direction vectors for snake turn
        private readonly Vector2D upVector = new Vector2D(0, -1);
        private readonly Vector2D rightVector = new Vector2D(1, 0);
        private readonly Vector2D downVector = new Vector2D(0, 1);
        private readonly Vector2D leftVector = new Vector2D(-1, 0);

        private Random random;

        /// <summary>
        /// Default constructor
        /// </summary>
        public World()
        {
            Players = new Dictionary<int, Snake>();
            Powerups = new Dictionary<int, Powerup>();
            Walls = new Dictionary<int, Wall>();
            diedPowerup = new HashSet<Powerup>();
            disconnectedSnakes = new HashSet<Snake>();
            random = new Random();

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_size">the world size</param>
        /// <param name="_playerID">player's ID</param>
        public World(int _size, int _playerID)
        {
            Players = new Dictionary<int, Snake>();
            Powerups = new Dictionary<int, Powerup>();
            Walls = new Dictionary<int, Wall>();
            diedPowerup = new HashSet<Powerup>();
            disconnectedSnakes = new HashSet<Snake>();
            WorldSize = _size;
            PlayerID = _playerID;
            random = new Random();
        }

        /// <summary>
        /// Sets the Extra mode on
        /// </summary>
        public void ExtraModeON()
        {
            IsExtraMode = true;
        }

        /// <summary>
        /// Resets the world to the initial state
        /// </summary>
        public void ResetWorld()
        {
            Players = new Dictionary<int, Snake>();
            Powerups = new Dictionary<int, Powerup>();
            Walls = new Dictionary<int, Wall>();
            diedPowerup = new HashSet<Powerup>();
            disconnectedSnakes = new HashSet<Snake>();
            PlayerID = -1;
            WorldSize = -1;
        }

        /// <summary>
        /// Gets the number of alive powers
        /// </summary>
        /// <returns>integer</returns>
        public int GetNumAlivePower()
        {
            int count = 0;
            foreach (Powerup power in Powerups.Values)
            {
                if (!power.Died)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Converts one Vector2D to a RectangleF with a given size
        /// </summary>
        /// <param name="position">Vector2D</param>
        /// <param name="size">The size of the RectangleF</param>
        /// <returns>RectangleF</returns>
        public static RectangleF VectorToRect(Vector2D position, float size)
        {
            return new RectangleF((float)position.GetX() - size / 2, (float)position.GetY() - size / 2, size, size);
        }


        /// <summary>
        /// Converts two Vector2D to a RectangleF with a given size
        /// </summary>
        /// <param name="v1">Vector2D</param>
        /// <param name="v2">Vector2D</param>
        /// <param name="size">the width or height of the RectangleF</param>
        /// <returns>RectangleF</returns>
        public static RectangleF VectorToRect(Vector2D v1, Vector2D v2, float size)
        {
            float heightOrWidth = (float)(v1 - v2).Length() + size;

            //Vertical
            if (v1.GetX() == v2.GetX())
            {
                if (v1.GetY() < v2.GetY())
                {
                    return new RectangleF((float)v1.GetX() - size / 2, (float)v1.GetY() - size / 2, size, heightOrWidth);
                }
                return new RectangleF((float)v1.GetX() - size / 2, (float)v2.GetY() - size / 2, size, heightOrWidth);

            }
            //Horizontal
            else
            {
                if (v1.GetX() < v2.GetX())
                {
                    return new RectangleF((float)v1.GetX() - size / 2, (float)v1.GetY() - size / 2, heightOrWidth, size);
                }
                return new RectangleF((float)v2.GetX() - size / 2, (float)v1.GetY() - size / 2, heightOrWidth, size);
            }
        }

        /// <summary>
        /// Converts a consecutive Vector2D points to continuous list of RectangleF
        /// Each neighboring RectangleF shares one point
        /// This is used for snake segments
        /// </summary>
        /// <param name="listVector">list of Vector2D</param>
        /// <param name="size">the size of the snake</param>
        /// <returns>a list of RectangleF</returns>
        public static List<RectangleF> VectorToRect(List<Vector2D> listVector, float size)
        {
            List<RectangleF> listRect = new List<RectangleF>();

            for (int i = 0; i < listVector.Count; i++)
            {
                if (i <= listVector.Count - 2)
                {
                    Vector2D v1 = listVector[i];
                    Vector2D v2 = listVector[i + 1];

                    float heightOrWidth = (float)(v1 - v2).Length() + size;

                    //Horizontal
                    if (v1.GetX() == v2.GetX())
                    {
                        if (v1.GetY() < v2.GetY())
                        {
                            listRect.Add(new RectangleF((float)v1.GetX() - size / 2, (float)v1.GetY() - size / 2, size, heightOrWidth));
                        }
                        else listRect.Add(new RectangleF((float)v1.GetX() - size / 2, (float)v2.GetY() - size / 2, size, heightOrWidth));

                    }
                    //Vertical
                    else
                    {
                        if (v1.GetX() < v2.GetX())
                        {
                            listRect.Add(new RectangleF((float)v1.GetX() - size / 2, (float)v1.GetY() - size / 2, heightOrWidth, size));
                        }
                        else listRect.Add(new RectangleF((float)v2.GetX() - size / 2, (float)v1.GetY() - size / 2, heightOrWidth, size));
                    }
                }
            }

            return listRect;
        }

        /// <summary>
        /// Checks if a given RectangleF is intersecting with all world objects
        /// </summary>
        /// <param name="thisRect">RectangleF</param>
        /// <returns>True, if it collides; otherwise, false</returns>
        private bool IsOverlappingWithAnyObjects(RectangleF thisRect)
        {
            foreach (Snake snake in Players.Values)
            {
                if (!snake.IsDead && IsOverlappingWith(thisRect, snake!.RectList))
                    return true;
            }
            foreach (Wall wall in Walls.Values)
            {
                if (IsOverlappingWith(thisRect, wall!.Rect))
                    return true;
            }
            foreach (Powerup powerup in Powerups.Values)
            {
                if (!powerup.Died && IsOverlappingWith(thisRect, powerup!.Rect))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a RectangleF is intersecting with another RectangleF
        /// </summary>
        /// <param name="thisRect">RectangleF</param>
        /// <param name="otherRect">RectangleF</param>
        /// <returns>True, if it collides; otherwise, false</returns>
        private bool IsOverlappingWith(RectangleF thisRect, RectangleF otherRect)
        {

            if (thisRect.IntersectsWith(otherRect))
                return true;

            return false;
        }

        /// <summary>
        /// Checks is a RectangleF is intersecting with any RectangleF in a RectangleF list
        /// </summary>
        /// <param name="thisRect">RectangleF</param>
        /// <param name="otherRectList">RectangleF list</param>
        /// <returns>True, if it collides; otherwise, false</returns>
        private bool IsOverlappingWith(RectangleF thisRect, List<RectangleF> otherRectList)
        {
            foreach (RectangleF otherRect in otherRectList)
            {
                if (thisRect.IntersectsWith(otherRect))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a powerup the world and updates its related fileds
        /// </summary>
        public void AddOneValidPowerup()
        {
            Powerup? powerup = null;
            while (powerup is null)
            {
                //get a random vector2d within the world
                Vector2D location = GenerateRandomVector();

                //vertor2d=>RectangleF
                RectangleF powerRect = World.VectorToRect(location, powerupSize);

                //cannot be overlapping with the walls, snakes, and existing powers
                if (!IsOverlappingWithAnyObjects(powerRect))
                {
                    int id = powerupCount++;
                    powerup = new Powerup(id, location);
                    Powerups.Add(id, powerup);

                }
            }
        }

        /// <summary>
        /// Gets a random frame number smaller than PowerRespawnRate
        /// </summary>
        /// <returns></returns>
        public int GetNewRandomFrame(int PowerRespawnRate)
        {
            return random.Next(0, PowerRespawnRate) + 1;
        }

        /// <summary>
        /// Generates a ramdom Vector2D within the world
        /// </summary>
        /// <returns></returns>
        private Vector2D GenerateRandomVector()
        {
            double maximum = WorldSize / 2;
            double minimum = -WorldSize / 2;

            //Random x and y for head's coordinates
            double x = random.NextDouble() * (maximum - minimum) + minimum;
            double y = random.NextDouble() * (maximum - minimum) + minimum;

            return new Vector2D(x, y);

        }

        /// <summary>
        /// Generates a random direction vector
        /// </summary>
        /// <returns>Vector2D</returns>
        private Vector2D GenerateRandomDirection()
        {
            int rand = random.Next(0, 4); // 0: NORTH (UP), 1: EAST (RIGHT), 2: SOUTH (DOWN), 3: WEST (LEFT)

            switch (rand)
            {
                case 0: return upVector;
                case 1: return rightVector;
                case 2: return downVector;
                case 3: return leftVector;
                default: return leftVector;
            }

        }

        /// <summary>
        /// Converts a string to a Vector2D direction
        /// </summary>
        /// <param name="movement"string command></param>
        /// <returns>Vector2D represents a direction</returns>
        public Vector2D GetMovementVector(string movement)
        {
            if (movement == "up")
                return upVector;
            else if (movement == "right")
                return rightVector;
            else if (movement == "down")
                return downVector;
            else return leftVector;
        }

        /// <summary>
        /// Checks snake's collison with snakes, walls, and foods
        /// </summary>
        /// <param name="snake">A snake</param>
        public void CheckCollision(Snake snake)
        {
            lock (this)
            {

                //Snake - Powerup 
                foreach (Powerup powerup in Powerups.Values)
                {
                    if (!powerup.Died && snake.IsSnakeCollidedWith(powerup))
                    {
                        powerup.Died = true;
                        diedPowerup.Add(powerup);
                        snake.IncreaseScore();

                        // Extra feature 2: In Extra Mode, snake is getting speed up by x2 instead of growing the length
                        if (IsExtraMode)
                            snake.SpeedUpModeON();
                        else snake.GrowingModeON();

                    }
                }

                //Snake - Snake
                foreach (Snake player in Players.Values)
                {
                    //self collision
                    if (player.ID == snake.ID)
                    {
                        if (snake.IsSelfCollided())
                        {
                            snake.SetSnakeDeath();
                            //Extra feature 1: Add a powerup at the head's position when it's died
                            SnakeDeathToPowerup(snake);
                            return;
                        }
                    }
                    //collsion with other snakes
                    else
                    {
                        //Extra feature 3: In Extra Mode, if the snake has the same color, they can't collide each other
                        if (IsExtraMode && player.ID % 8 == snake.ID % 8)
                        {
                            continue;
                        }
                        else if (!player.IsDead && snake.IsSnakeCollidedWithOtherSnake(player.RectList))
                        {
                            snake.SetSnakeDeath();
                            //Extra feature 1: Add a powerup at the head's position when it's died
                            SnakeDeathToPowerup(snake);
                            return;
                        }
                    }
                }

                //Snake - Wall
                foreach (Wall wall in Walls.Values)
                {
                    if (snake.IsSnakeCollidedWith(wall))
                    {
                        snake.SetSnakeDeath();
                        //Extra feature 1: Add a powerup at the head's position when it's died
                        SnakeDeathToPowerup(snake);
                        return;
                    }
                }



            }
        }

        //Extra feature 1
        /// <summary>
        /// Turns a dead snake to a powerup
        /// </summary>
        /// <param name="snake"></param>
        public void SnakeDeathToPowerup(Snake snake)
        {
            int id = powerupCount++;

            //Add a super powerup at (the head's position - head's size) when it die
            Vector2D respawnPosition = snake.GetHead() - (snake.Direction! * snake.size);

            Powerup powerup = new Powerup(id, respawnPosition);
            Powerups.Add(id, powerup);
        }

        /// <summary>
        /// Adds a new snake to the world and updates its related fields
        /// </summary>
        /// <param name="id">player id</param>
        /// <param name="name">player's name</param>
        /// <returns></returns>
        public Snake AddOneValidSnake(int id, string name, int SnakeSpeed, int SnakeLength, int SnakeGrowth)
        {
            Snake? snake = null;
            while (snake is null)
            {
                Vector2D respawnPosition = GenerateRandomVector();
                Vector2D direction = GenerateRandomDirection();

                //I am gonna fix this one later
                Vector2D tail = respawnPosition - direction * (SnakeLength - snakeSize); // 120 - 10 = 110

                //For Checking 
                RectangleF snakeRect = World.VectorToRect(tail, respawnPosition, snakeSize);

                //Could abtract later
                if (!IsOverlappingWithAnyObjects(snakeRect))
                {
                    snake = new Snake(id, name, new List<Vector2D>() { tail, respawnPosition }, direction, 0, false, true, false, true);
                    //Set Speed, Length, Growth for Snake
                    snake.SetSnakeProperties(SnakeSpeed, SnakeLength, SnakeGrowth);
                    //snake.GrowingModeON();
                    if (Players.ContainsKey(id))
                    {
                        Players[id] = snake;
                    }
                    else Players.Add(id, snake);

                    return snake;
                }

            }

            return snake;
        }

    }
}
