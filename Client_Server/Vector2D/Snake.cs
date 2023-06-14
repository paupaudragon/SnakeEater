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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Intrinsics;
using System.Reflection.Metadata.Ecma335;
using System.Net.Security;
using System.Net;

namespace SnakeGame.Models
{
    /// <summary>
    /// Snake Class represnets snakes in the game
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Snake
    {
        [JsonProperty("snake")]
        public int ID { get; private set; }
        [JsonProperty("name")]
        public string? name;
        [JsonProperty("body")]
        public List<Vector2D>? Position { get; private set; }
        [JsonProperty("dir")]
        public Vector2D? Direction { get; private set; }
        [JsonProperty("score")]
        public int Score { get; private set; }
        [JsonProperty("died")]
        public bool IsDead { get; private set; }
        [JsonProperty("alive")]
        public bool IsAlive { get; private set; }
        [JsonProperty("dc")]
        public bool IsDisConnected { get; private set; }
        [JsonProperty("join")]
        public bool IsJoin { get; private set; }

        //A list of RectangleF representing a snake
        public List<RectangleF> RectList { get; private set; } = new();

        //used for avoiding snake overlapping with itself
        public Vector2D previousDirection { get; private set; } = new();

        /**********Added fields for extra features*************/
        public int RespawnFrame { get; private set; } = -1;
        public int GrowthFrame { get; private set; } = -1;
        public int SpeedUpFrame { get; private set; } = -1;
        public bool IsGrowing { get; private set; } = false;
        public bool IsSpeedUp { get; private set; } = false;
        public int Speed { get; private set; } = -1;
        public int Length { get; private set; } = -1;
        public int GrowthRate { get; private set; } = -1;
        public int size { get; private set; } = World.snakeSize;


        /// <summary>
        /// Default Constructor
        /// </summary>
        public Snake() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Unique Id for each snake</param>
        /// <param name="list">vector<2D> list representing the tail and head position of a snake</param>
        /// <param name="angle">The angel of the snake's head direction</param>
        /// <param name="score">the score of a snake</param>
        /// <param name="IsDead">a boolan representing if the snake is dead</param>
        /// <param name="IsDisConnected">a boolean representing is the snake lost connection with the server</param>
        /// <param name="IsJoin">a boolean representing if the snake is joined by another snake</param>
        /// <param name="name">snake's name</param>
        public Snake(int id, string name, List<Vector2D> list, Vector2D direction, int score, bool IsDead, bool IsAlive, bool IsDisConnected, bool IsJoin)
        {

            ID = id;
            this.name = name;
            Position = list;
            this.IsAlive = IsAlive;
            this.IsDead = IsDead;
            this.IsDisConnected = IsDisConnected;
            this.IsJoin = IsJoin;
            this.Score = score;
            this.Direction = new Vector2D(direction);

            //if the snake is first initialized, use the same direction as prevousDirection for simplicity
            previousDirection = direction;

            //initialize the RectangleF list for this snake
            UpdateRect();
        }


        /// <summary>
        /// Sets the snake's speed, length, growth. 
        /// This is useful for extra feature implementation 
        /// </summary>
        /// <param name="speed">snake's speed</param>
        /// <param name="length">snake's initial length</param>
        /// <param name="growth">snake's growth length per food</param>
        public void SetSnakeProperties(int speed, int length, int growth)
        {
            this.Speed = speed;
            this.Length = length;
            this.GrowthRate = growth;
        }

        /// <summary>
        /// Checks is a snake has moved compared to prior status.
        /// 
        /// </summary>
        /// <param name="snake">the snake at a most recent time</param>
        /// <returns>True, if it moved; otherwse false.</returns>
        public bool IsMoving(List<Vector2D>? newPosition)
        {
            if (newPosition is not null && this.Position is not null)
                return !Enumerable.SequenceEqual(newPosition, this.Position);

            return false;
        }

        /// <summary>
        /// Gets the head position of a snake
        /// </summary>
        /// <returns>a Vector2D rpresentings head position</returns>
        public Vector2D GetHead()
        {
            return Position![Position.Count - 1];
        }

        /// <summary>
        /// Gets the head position of a snake
        /// </summary>
        /// <returns>a Vector2D rpresentings head position</returns>
        public Vector2D GetTail()
        {
            return Position![0];
        }

        /// <summary>
        /// Sets the snake to disconnected state
        /// </summary>
        public void SetDisConnectecd()
        {
            IsDisConnected = true;
            IsJoin = false;
        }

        /// <summary>
        /// Sets snake to death state.
        /// </summary>
        public void SetSnakeDeath()
        {
            IsDead = true;
            IsAlive = false;
            Score = 0;
            GrowthFrame = -1;
            SpeedUpFrame = -1;
            IsGrowing = false;
            IsSpeedUp = false;

        }

        /// <summary>
        /// Updates te snake's Respawn frame, so it knows when to end the death effect
        /// </summary>
        public void UpdateRespawnFrame()
        {
            //the first frame (IsDead && !IsAlive)
            //the second frame to the last frame: (!IsDead && !IsAlive)
            if (IsDead || !IsAlive)
            {
                RespawnFrame++;
            }
            else
            {
                RespawnFrame = 0;
            }
        }


        /// <summary>
        /// Resets the repawnFrame to 0.
        /// </summary>
        public void ResetRespawnFrame()
        {
            RespawnFrame = 0;
        }

        /// <summary>
        /// Increases the snake score by 1
        /// </summary>
        public void IncreaseScore()
        {
            Score++;
        }


        /// <summary>
        /// Sets the speed for speed up mode
        /// </summary>
        public void SpeedUpModeON()
        {

            if (!IsSpeedUp)
            {
                Speed *= 2; // x2 the speed
                IsSpeedUp = true;
                SpeedUpFrame++;
            }
            else
            {
                SpeedUpFrame -= 180; // extend for 3 seconds                                        
            }

        }

        /// <summary>
        /// Sets the growing mode
        /// </summary>
        public void GrowingModeON()
        {
            lock (this)
            {
                if (!IsGrowing)
                {
                    IsGrowing = true;
                }
                else
                {
                    GrowthFrame -= GrowthRate;
                }
            }
        }



        /// <summary>
        /// Updates snake's Position points based on a given direction.
        /// </summary>
        /// <param name="direction">Vector2D</param>
        public void UpdateSnakeMovement(Vector2D newRirection)
        {

            if (Position!.Count < 2)
                return;


            if (IsSpeedUp == true)
            {
                SpeedUpFrame++;
                if (SpeedUpFrame > 180) // Last for 3 seconds
                {
                    //Reset Speed Up Mode
                    Speed /= 2;
                    IsSpeedUp = false;
                    SpeedUpFrame = -1;
                }
            }


            //snake is turning
            if (!newRirection.Equals(Direction))
            {
                previousDirection = Direction!;
                Direction = newRirection;


                //Create new segment for the snake with the 
                Vector2D newSegment = new Vector2D(Position![Position.Count - 1]);

                //Add velocity for the head position
                newSegment += (Direction * Speed);

                //Check Wrapping Around
                //If it's not wrapping around, add newsegment, otherwise add a segment in the other world 
                if (!IsWrappingAround())
                {
                    Position.Add(newSegment);
                }
            }
            else // snake is not turning
            {
                //Moving the head
                Position![Position.Count - 1] += (Direction * Speed);
                IsWrappingAround();
            }



            //Moving the tail
            if (!IsGrowing)
            {
                Vector2D bodyDirection = Vector2D.GetDirection(Position[0], Position[1]);
                Position[0] += (bodyDirection * Speed);
                Vector2D afterMoveDirection = Vector2D.GetDirection(Position[0], Position[1]);
                //Remove the last Vector whenever the tail catch up the forward Vector
                if (Position[0].Equals(Position[1]) || bodyDirection.IsOppositeCardinalDirection(afterMoveDirection))
                {
                    Position.Remove(Position[0]);
                    //If the new tail in the list is at the boundary of the world -> remove it as well
                    if (Position[0].X >= World.WorldSize / 2 || Position[0].X <= -World.WorldSize / 2 || Position[0].Y >= World.WorldSize / 2 || Position[0].Y <= -World.WorldSize / 2)
                    {
                        Position.Remove(Position[0]);
                    }
                }
            }
            else
            {
                GrowthFrame++;

                //Reset the growthFrame and stop growing
                if (GrowthFrame == GrowthRate)
                {
                    IsGrowing = false;
                    GrowthFrame = -1;
                }
            }

            UpdateRect();
        }

        /// <summary>
        /// Checks if the game needs to wrap around
        /// </summary>
        /// <returns>Ture, if it needs to wrap around; otherwise, false.</returns>
        private bool IsWrappingAround()
        {
            //Check Wrapping Around
            if (GetHead().X >= World.WorldSize / 2 || GetHead().X <= -World.WorldSize / 2    // X = -1000 || X = 1000
                || GetHead().Y >= World.WorldSize / 2 || GetHead().Y <= -World.WorldSize / 2) // Y = -1000 || Y = 1000
            {
                Vector2D v1 = Position![Position.Count - 1] - (Direction! * World.WorldSize);
                Position.Add(v1);
                Position.Add(v1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks is snake collides with another snake.
        /// </summary>
        /// <param name="otherRectList">a list a RectangleF representing another snake</param>
        /// <returns>True, if collides; otherwise, false</returns>
        public bool IsSnakeCollidedWithOtherSnake(List<RectangleF> otherRectList)
        {
            if (otherRectList.Count == 0)
                return false;

            foreach (RectangleF rect in otherRectList)
            {
                if (IsCollided(rect))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a snake collides with itself.
        /// </summary>
        /// <returns>True, if collides; otherwise, false</returns>
        public bool IsSelfCollided()
        {
            if (RectList.Count >= 4)
            {
                //Ignore the last 3 rectangles
                for (int i = 0; i < RectList.Count - 3; i++)
                {
                    if (IsCollided(RectList[i]))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a snake eats a food
        /// </summary>
        /// <param name="powerup">a Powerup Object</param>
        /// <returns>True, if snake eats a food; otherwise, false</returns>
        public bool IsSnakeCollidedWith(Powerup powerup)
        {
            return ((GetHead() - powerup.Position!).Length() <= powerup.size);
        }

        /// <summary>
        /// Checks if a snake collides with a wall
        /// </summary>
        /// <param name="wall">a wall object</param>
        /// <returns>True, if collides; otherwise, false</returns>
        public bool IsSnakeCollidedWith(Wall wall)
        {
            RectangleF headRect = World.VectorToRect(GetHead(), size);
            if (wall.Rect.IntersectsWith(headRect))
                return true;

            return false;
        }

        /// <summary>
        /// Checking if the head's rectangle and other snake's rectangle is intersecting each other
        /// and if the other snake' rectangle is containing the head's point of this snake
        /// </summary>
        /// <param name="rect">object's rectangle</param>
        /// <returns>True, if collides; otherwise, false</returns>
        public bool IsCollided(RectangleF rect)
        {
            RectangleF headRect = World.VectorToRect(GetHead(), size);
            PointF headPoint = GetHeadPoint();
            if (rect.IntersectsWith(headRect) && rect.Contains(headPoint))
                return true;

            return false;
        }

        /// <summary>
        /// Returns a PointF representing a snake's head with offset of its width/2
        /// </summary>
        /// <returns>head's point</returns>
        public PointF GetHeadPoint()
        {
            Vector2D point = GetHead() + Direction! * (size / 2);
            return new PointF((float)point.GetX(), (float)point.GetY());
        }

        /// <summary>
        /// Checks if snake has travelled over 10 units
        /// This is used by avoiding suicidal turn
        /// </summary>
        /// <returns>True, if it has travelled over 10 units; otherwise, false</returns>
        public bool IsTravelledMoreThanTen()
        {
            return (GetHead() - Position![Position.Count - 2]).Length() > 10;
        }

        /// <summary>
        /// Updates the RectangleF list of a snake
        /// </summary>
        public void UpdateRect()
        {
            RectList = World.VectorToRect(Position!, size);

        }
    }
}
