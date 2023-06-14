/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using Microsoft.Maui.Graphics;
using SnakeGame;
using SnakeGame.Models;
using System;

namespace SnakeGame;

/// <summary>
/// View class for Snake Game GUI  
/// </summary>
public partial class MainPage : ContentPage
{

    private readonly GameController controller;

    /// <summary>
    /// Constructor
    /// </summary>
    public MainPage()
    {
        //Initialize the GUI
        InitializeComponent();
        Dispatcher.Dispatch(() => graphicsView.Invalidate());

        //Initialize Controller related members
        controller = new();
        controller.Connected += HandleConnected;
        controller.UpdateArrived += HandleUpdate;
        controller.Error += ShowError;
        controller.EnableCommand += GetCommand;

        //Disable user command
        keyboardHack.IsEnabled = false;

    }

    /******************************Game Controller code **************************/

    /// <summary>
    /// Gets command from user's input.
    /// Handler  Gamecontroller's EnableCommand event.
    /// </summary>
    private void GetCommand()
    {
        //Enable user command input
        Dispatcher.Dispatch(() => keyboardHack.IsEnabled = true);

        //Focus the Entry to get more command
        Dispatcher.Dispatch(() => keyboardHack.Focus());

    }

    /// <summary>
    /// Updates the game panel.
    /// Handler GameController's UpdateArrived event.
    /// </summary>
    private void HandleUpdate()
    {
        worldPanel.IsConnected = true;
        //if (controller.startDeath)
        //{
        //    worldPanel.explosionFrameCounter++;
        //}
        //Inform the world panel to draw
        Dispatcher.Dispatch(() => graphicsView.Invalidate());

    }

    /// <summary>
    /// Sets the world in world panel to the world in GameController and draw.
    /// Handler for the controller's Connected event
    /// </summary>
    private void HandleConnected()
    {
        //Set the world for worldPanel
        worldPanel.SetWorld(controller.GetWorld());

        //Draw the world
        Dispatcher.Dispatch(() => graphicsView.Invalidate());

        // Extra feature 1 - Added 11/26
        Dispatcher.Dispatch(() => connectButton.Text = "Connected");
    }

    /// <summary>
    /// Displays error
    /// </summary>
    /// <param name="errMessage">Error message</param>
    private void NetworkErrorHandler(string errMessage)
    {
        DisplayAlert("Error", errMessage, "OK");
    }

    /// <summary>
    /// Dsiplays error
    /// Handler for the controller's Error event
    /// </summary>
    /// <param name="err">Error message string </param>
    private void ShowError(string err)
    {
        // Diaplay the error
        Dispatcher.Dispatch(() => NetworkErrorHandler(err));

        // Re-enable the user to connect 
        Dispatcher.Dispatch(
          () =>
          {
              // Extra feature 2: If the server was terminated when player is playing,
              // player can reconnect to continue the game
              ResetGuiSetting();

              //Resets the world status
              controller.GetWorld().ResetWorld();

              //Draw the welcome page
              Dispatcher.Dispatch(() => graphicsView.Invalidate());
          });
    }

    /// <summary>
    /// Helper method for reseting the GUI when errors pop up
    /// </summary>
    private void ResetGuiSetting()
    {
        worldPanel.IsConnected = false;
        connectButton.IsEnabled = true;

        // Change the button's look to remind the user
        connectButton.Text = "Reconnect";
        connectButton.BackgroundColor = Colors.Green;
        connectButton.TextColor = Colors.White;

        //convert text fields and keyboard back to initial state
        serverText.IsEnabled = true;
        nameText.IsEnabled = true;
        keyboardHack.IsEnabled = false;
    }

    /*********************************GUI events Code***************************/

    /// <summary>
    /// Focuses on user command entry after the window has been scrolled.
    /// Handler for ScrollView Tapped event 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// Translates key board input into server command.
    /// Handler for Entry's TextChanged event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w" || text == "8")
        {
            controller.SendCommand("{ \"moving\":\"up\"}\n");

        }
        if (text == "a" || text == "4")
        {
            controller.SendCommand("{ \"moving\":\"left\"}\n");

        }
        if (text == "s" || text == "2")
        {
            controller.SendCommand("{ \"moving\":\"down\"}\n");

        }
        if (text == "d" || text == "6")
        {
            controller.SendCommand("{ \"moving\":\"right\"}\n");
        }
        else
        {
            controller.SendCommand("{ \"moving\":\"none\"}\n");
        }

        entry.Text = "";
    }

    /// <summary>
    /// Attempts to make connection to the server through GameController.
    /// Event handler for the connect button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {

        //Server ip cannot be an empty string 
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }

        //Player name cannot be an empty string
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }

        //Player name length < 16
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        // Disable the controls and try to connect
        connectButton.IsEnabled = false;
        serverText.IsEnabled = false;
        nameText.IsEnabled = false;
        connectButton.BackgroundColor = Colors.Grey;

        //Connect to server, passing server name and player name
        controller.Connect(serverText.Text, nameText.Text);
        keyboardHack.Focus();
    }

    /// <summary>
    /// Hnadler for ContenPage's Focused event
    /// </summary>
    /// <param name="sender">ContentPage</param>
    /// <param name="e">focus event</param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }

    /// <summary>
    /// Helper method for size settings
    /// </summary>
    /// <param name="size">view size</param>
    private void SetViewSize(int size)
    {
        graphicsView.WidthRequest = size;
        graphicsView.HeightRequest = size;
        worldPanel.viewSize = size;
    }

    /// <summary>
    /// Sets the view size to 600x600
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetSmallSize(object sender, EventArgs e)
    {
        SetViewSize(600);
        //Focus the keyboard.
        Dispatcher.Dispatch(() => keyboardHack.Focus());
    }

    /// <summary>
    /// Sets the view size to 1200x1200
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetLargeSize(object sender, EventArgs e)
    {
        SetViewSize(1200);
        //Focus the keyboard.
        Dispatcher.Dispatch(() => keyboardHack.Focus());
    }

    /// <summary>
    /// Sets the view size to 900x900
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetDefaultSize(object sender, EventArgs e)
    {
        SetViewSize(900);
        //Focus the keyboard.
        Dispatcher.Dispatch(() => keyboardHack.Focus());
    }

    /// <summary>
    /// Sets the game theme to Green Forest
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetDefaultTheme(object sender, EventArgs e)
    {
        worldPanel.IsDefaultTheme = true;
        //Draw the world
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
        //Focus the keyboard.
        Dispatcher.Dispatch(() => keyboardHack.Focus());
    }

    /// <summary>
    /// Setst the theme to Blue Pixel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetBluePixelTheme(object sender, EventArgs e)
    {
        worldPanel.IsDefaultTheme = false;
        //Draw the world
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
        //Focus the keyboard.
        Dispatcher.Dispatch(() => keyboardHack.Focus());
    }

    /// <summary>
    /// Displays About menu content
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutClicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
     "SnakeGame solution\n" +
     "Artwork by Jolie Uk and Alex Smith\n" +
     "Game design by Daniel Kopta and Travis Martin\n" +
     "Implementation by Andy and Tingting\n" +
       "CS 3500 Fall 2022, University of Utah", "OK");
    }

    /// <summary>
    /// Displays Help menu content 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HelpClicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                    "W or 8:\t\t Move up\n" +
                    "A or 4:\t\t Move left\n" +
                    "S or 2:\t\t Move down\n" +
                    "D or 6:\t\t Move right\n",
                    "OK");
    }

}