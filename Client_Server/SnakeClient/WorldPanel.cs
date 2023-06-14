/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using Microsoft.Maui.Controls;
using SnakeGame.Models;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Graphics;
using System.Text;

namespace SnakeGame;
/// <summary>
/// A class represents the game view.
/// </summary>
public class WorldPanel : ScrollView, IDrawable
{
    //IImage variables
    private IImage wall;

    private IImage berryFood;
    private IImage greenForestMap;
    private IImage bluePixelMap;
    private IImage starFood;
    private IImage death;
    private IImage welcome;
    private IImage background;
    private IImage powerupFood;

    private IImage head;
    private IImage yellow;
    private IImage red;
    private IImage pink;
    private IImage lightgreen;
    private IImage brown;
    private IImage cyan;
    private IImage orange;
    private IImage purple;

    private IImage explosion1;
    private IImage explosion3;
    private IImage explosion5;
    private IImage explosion9;

    private bool initializedForDrawing = false;
    private World theWorld;

    // A delegate for DrawObjectWithTransform
    // Methods matching this delegate can draw whatever they want onto the canvas  
    public delegate void ObjectDrawer(object o, ICanvas canvas);

    private GraphicsView graphicsView = new();
    public int viewSize = 900;

    //Decide if is default theme
    public bool IsDefaultTheme { get; set; }

    //Decide if the server is connected
    public bool IsConnected { get; set; }

    //Explosion frame tempFrameCounterForSnake for each snake
    //snake id is the key, tempFrameCounterForSnake is the value
    private Dictionary<int, int> explosionFrameCounter= new();
    private int tempFrameCounterForSnake = 0;


#if MACCATALYST
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));


    }
#endif

    /// <summary>
    /// Constructor
    /// </summary>
    public WorldPanel()
    {
        BackgroundColor = Colors.Black;
        graphicsView.Drawable = this;
        graphicsView.HeightRequest = viewSize;
        graphicsView.WidthRequest = viewSize;
        graphicsView.BackgroundColor = Colors.Black;
        this.Content = graphicsView;

        IsConnected = false;
        IsDefaultTheme = true;
    }


    /// <summary>
    /// Sets the world.
    /// </summary>
    /// <param name="theWorld">a World</param>
    public void SetWorld(World theWorld)
    {
        this.theWorld = theWorld;
    }



    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object'snake position in world space</param>
    /// <param name="worldY">The Y component of the object'snake position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();
        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The snake to be drawn</param>
    /// <param name="canvas">Canvas</param>
    private void SnakeHeadDrawer(object o, ICanvas canvas)
    {
        double width = 10;
        double height = 10;
        canvas.DrawImage(head, -(float)width / 2, -(float)height / 2, (float)width, (float)height);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate that draws the death animation
    /// </summary>
    /// <param name="o">The snake to be drawn</param>
    /// <param name="canvas">canvas</param>
    private void DeathSnakeDrawer(object o, ICanvas canvas)
    {
        Snake s = o as Snake;
        double width = 200;
        double height = 150;
        if (explosionFrameCounter[s.ID] > 0 && explosionFrameCounter[s.ID] < 4)
            canvas.DrawImage(explosion1, -(float)width / 2, -(float)height / 2, (float)width, (float)height);
        else if (explosionFrameCounter[s.ID] > 3 && explosionFrameCounter[s.ID] < 7)
            canvas.DrawImage(explosion3, -(float)width / 2, -(float)height / 2, (float)width, (float)height);
        else if (explosionFrameCounter[s.ID] > 6 && explosionFrameCounter[s.ID] < 10)
            canvas.DrawImage(explosion5, -(float)width / 2, -(float)height / 2, (float)width, (float)height);
        else if (explosionFrameCounter[s.ID] > 9 && explosionFrameCounter[s.ID] < 13)
            canvas.DrawImage(explosion9, -(float)width / 2, -(float)height / 2, (float)width, (float)height);
    }


    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas">canvas</param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        Powerup powerup = o as Powerup;
        double width = 16;
        double height = 16;
      
        canvas.DrawImage(powerupFood, -(float)width / 2, -(float)height / 2, (float)width, (float)height);

    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">the wall the draw</param>
    /// <param name="canvas">canvas</param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        double width = 50;
        double height = 50;
        canvas.DrawImage(wall, -(float)width / 2, -(float)height / 2, (float)width, (float)height);
    }

    /// <summary>
    /// Gets the angel of a snake
    /// </summary>
    /// <param name="player">the snake</param>
    /// <returns>a float number representing angel</returns>
    private float GetAngle(Snake player)
    {
        Vector2D directionVector = player.Direction;
        directionVector.Normalize();
        return directionVector.ToAngle();
    }

    /// <summary>
    /// Combines the snake'snake head and body with matching color
    /// </summary>
    /// <param name="canvas">canvas</param>
    /// <param name="player">the snake</param>
    private void DoSnakeSetting(ICanvas canvas, Snake player)
    {
        //Draw the body by ID
        int remainder = player.ID % 8;

        canvas.StrokeSize = 10;
        canvas.StrokeLineCap = LineCap.Round;
        switch (remainder)
        {
            case 0:
                head = yellow;
                canvas.StrokeColor = Color.FromRgba(250, 192, 90, 255); // Yellow
                break;
            case 1:
                head = red;
                canvas.StrokeColor = Color.FromRgba(250, 35, 65, 255); // Red
                break;
            case 2:
                head = purple;
                canvas.StrokeColor = Color.FromRgba(183, 119, 240, 255); // Purple
                break;
            case 3:
                head = pink;
                canvas.StrokeColor = Color.FromRgba(250, 119, 172, 255); // Pink
                break;
            case 4:
                head = lightgreen;
                canvas.StrokeColor = Color.FromRgba(114, 252, 131, 255); // Light Green 
                break;
            case 5:
                head = orange;
                canvas.StrokeColor = Color.FromRgba(250, 131, 79, 255); // Orange
                break;
            case 6:
                head = brown;
                canvas.StrokeColor = Color.FromRgba(219, 105, 69, 255); // Brown
                break;
            case 7:
                head = cyan;
                canvas.StrokeColor = Color.FromRgba(114, 219, 214, 255); // Cyan
                break;
            default:
                head = yellow;
                canvas.StrokeColor = Color.FromRgba(250, 192, 90, 255); // Yellow
                break;
        }
    }

    /// <summary>
    /// Draws the snake with correct state in the world coordinate
    /// </summary>
    /// <param name="canvas">canvas</param>
    /// <param name="player">the snake</param>
    private void DrawingSnakes(ICanvas canvas, Snake player)
    {
        //decides the snake color
        DoSnakeSetting(canvas, player);

        //snake death drawing
        if (!player.IsAlive)
        {
            //frame tempFrameCounterForSnake for each snake would increment by one every time receive a !IsAlive snake
            tempFrameCounterForSnake = explosionFrameCounter[player.ID];
            explosionFrameCounter.Remove(player.ID);
            explosionFrameCounter.Add(player.ID, ++tempFrameCounterForSnake);

            Vector2D headPosition = player.GetHead();
            DrawObjectWithTransform(canvas, player,
        headPosition.GetX(), headPosition.GetY(), 0,
        DeathSnakeDrawer);
            return;
        }
        else// snake respawns, reset frame to 0
        {
            explosionFrameCounter.Remove(player.ID);
        }

        for (int i = 0; i < player.Position.Count; i++)
        {
            if ((i + 1) <= player.Position.Count - 1)// if the vertex is not the end
            {
                Vector2D vertex1 = player.Position[i];
                Vector2D vertex2 = player.Position[i + 1];

                //Calculate the length to avoid draw from one edge to another edge
                double length = Math.Sqrt(Math.Pow(vertex1.GetX() - vertex2.GetX(), 2) + Math.Pow(vertex1.GetY() - vertex2.GetY(), 2));

                if (length < World.WorldSize)
                {
                    //Draw the body of snake
                    canvas.DrawLine((float)vertex1.GetX(), (float)vertex1.GetY(), (float)vertex2.GetX(), (float)vertex2.GetY());

                    //Draw the head at the last point - 1
                    if (i == (player.Position.Count - 2))
                    {
                        DrawObjectWithTransform(canvas, player,
                        vertex2.GetX(), vertex2.GetY(), GetAngle(player),
                        SnakeHeadDrawer);
                    }
                }

            }
        }


    }

    /// <summary>
    /// Draws the snake'snake name and score
    /// </summary>
    /// <param name="canvas">canvas</param>
    /// <param name="player">the snake</param>
    private void DrawSnakeInfo(ICanvas canvas, Snake player)
    {
        canvas.FontColor = Colors.White;
        Vector2D headPostion = player.GetHead();
        string nameScoreText = player.name + " (" + player.Score + ")";
        canvas.DrawString(nameScoreText, (float)headPostion.GetX(), (float)headPostion.GetY() + 23, HorizontalAlignment.Center);
    }

    /// <summary>
    /// Draws walls in the world coordinate
    /// </summary>
    /// <param name="canvas">canvas</param>
    /// <param name="wall">the wall</param>
    private void DrawingWalls(ICanvas canvas, Wall wall)
    {

        int increment;

        //wall'snake horizontal part
        if (wall.IsHorizontal)
        {
            //increment = 50 means draw along the y axis arrow
            //increment = -50 means draw against the y axis arrow
            increment = GetWallDrawIncrement(wall.Position1.GetX(), wall.Position2.GetX());

            double drawX;
            for (int i = 0; i < wall.GetWidth() / 50; i++)
            {
                drawX = i * increment + wall.Position1.GetX();//
                DrawObjectWithTransform(canvas, wall,
            drawX, wall.Position1.GetY(), 0,
            WallDrawer);
            }
        }
        //wall'snake vertical part
        else
        {
            //increment = 50 means draw along the x axis arrow
            //increment = -50 means draw against the x axis arrow
            increment = GetWallDrawIncrement(wall.Position1.GetY(), wall.Position2.GetY());
            double drawY;
            for (int i = 0; i < wall.GetHeight() / 50; i++)
            {
                drawY = i * increment + wall.Position1.GetY();
                DrawObjectWithTransform(canvas, wall,
            wall.Position1.GetX(), drawY, 0,
            WallDrawer);
            }
        }
    }

    /// <summary>
    /// Gets the wall increment for next block of wall drawing 
    /// </summary>
    /// <param name="start">the x or y coordinate of wall'snake one side</param>
    /// <param name="end">the x or y coordinate of wall'snake the other side</param>
    /// <returns></returns>
    private static int GetWallDrawIncrement(double start, double end)
    {
        if (start > end)
            return -50;
        else return 50;
    }

    /// <summary>
    /// Preloads the graphics needed in this game.
    /// </summary>
    private void InitializeDrawing()
    {
        wall = loadImage("WallSprite.png"); //our sprite is WallSprite.png
        death = loadImage("death.png");
        welcome = loadImage("welcome.png");

        //preload all theme related graphics
        greenForestMap = loadImage("Background.png");
        berryFood = loadImage("berry.png");
        bluePixelMap = loadImage("backGround2.jpg");
        starFood = loadImage("star.png");
        //default is green forest theme
        background = greenForestMap;
        powerupFood = berryFood;

        //snake head image
        head = loadImage("yellow.png"); // default color is yellow
        cyan = loadImage("cyan.png");
        yellow = loadImage("yellow.png");
        red = loadImage("red.png");
        purple = loadImage("purple.png");
        pink = loadImage("pink.png");
        lightgreen = loadImage("lightgreen.png");
        orange = loadImage("orange.png");
        brown = loadImage("brown.png");

        //explosion
        explosion1 = loadImage("explosion1.png");
        explosion3 = loadImage("explosion3.png");
        explosion5 = loadImage("explosion5.png");
        explosion9 = loadImage("explosion9.png");


        initializedForDrawing = true;
    }

    /// <summary>
    /// Adds each snake id and a 0 frame to the dictionary.
    /// </summary>
    /// <param name="dictionary">a dictionary</param>
    /// <param name="snake">a snake</param>
    private void AddSnakeFrameToDictionary(Dictionary<int, int> dictionary, Snake snake)
    {
        if (!dictionary.TryGetValue(snake.ID, out tempFrameCounterForSnake))
            dictionary.Add(snake.ID, 0);
    }


    /// <summary>
    /// This runs whenever the drawing panel is invalidated and draws the game
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!initializedForDrawing)
        {
            InitializeDrawing();
        }

        // undo any leftover transformations from last frame
        canvas.ResetState();

        if (IsConnected && theWorld.PlayerID > -1 && World.WorldSize > -1) // Make sure the ID and world size have received before drawing anything
        {

            lock (theWorld)
            {
                // center the view by the snake postion
                if (theWorld.Players.ContainsKey(theWorld.PlayerID))
                {
                    Vector2D myPostion = theWorld.Players[theWorld.PlayerID].GetHead();
                    float playerX = (float)myPostion.GetX(); // Get the head positionX which is the last
                    float playerY = (float)myPostion.GetY(); // Get the head positionY which is the last

                    //The size and position is confirmed by PS8 instruction
                    canvas.Translate(-playerX + ((float)viewSize / 2), -playerY + ((float)viewSize / 2)); // Focus on the head position frame to frame
                }

                //Allows to change the theme any time before and during the game
                if (IsDefaultTheme)
                {
                    background = greenForestMap;
                    powerupFood = berryFood;
                }
                else
                {
                    background = bluePixelMap;
                    powerupFood = starFood;
                }

                //Draws the background in the middle of the world.
                canvas.DrawImage(background, -World.WorldSize / 2, -World.WorldSize / 2, World.WorldSize, World.WorldSize);

                //Draws all the walls in the world.
                foreach (var wall in theWorld.Walls.Values)
                {
                    DrawingWalls(canvas, wall);
                }

                //Draws all the snakes in the world.
                foreach (var player in theWorld.Players.Values)
                {
                    AddSnakeFrameToDictionary(explosionFrameCounter, player);//for death animation
                    DrawingSnakes(canvas, player);
                    if(player.IsAlive)
                        DrawSnakeInfo(canvas, player);
                }
                //Draws all the foods in the world
                foreach (var powerup in theWorld.Powerups.Values)
                {

                    DrawObjectWithTransform(canvas, powerup,
                     powerup.Position.GetX(), powerup.Position.GetY(), 0,
                     PowerupDrawer);

                }

            }
        }
        else //if not connected, display welcome page
        {
            canvas.Translate((float)viewSize / 2, (float)viewSize / 2);
            canvas.DrawImage(welcome, -viewSize / 2, -viewSize / 2, viewSize, viewSize);
        }

    }

}