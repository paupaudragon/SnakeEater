using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using GameLab.Models;

namespace GameLab
{
  public class GameController
  {
    // World is a simple container for Players and Powerups
    private World theWorld;

    // Although we aren't actually doing any networking,
    // this FakeServer object will simulate game updates coming 
    // from an asynchronous server.
    private FakeServer server;
      
    // A delegate and event to fire when the controller
    // has received and processed new info from the server
    public delegate void GameUpdateHandler();
    public event GameUpdateHandler UpdateArrived;

    public GameController()
    {
      theWorld = new World( 500 );
      server = new FakeServer( theWorld.Size );
      server.ServerUpdate += UpdateCameFromServer;
    }

    public World GetWorld()
    {
      return theWorld;
    }

    public void Run()
    {
      Thread t = new Thread( server.Run );
      t.Start();
    }


    /// <summary>
    /// Handler for when data arrives from the fake server
    /// </summary>
    /// <param name="players">the updated players</param>
    /// <param name="powerups">the updated powerups</param>
    private void UpdateCameFromServer( IEnumerable<Player> players, IEnumerable<Powerup> powerups )
    {
      //Random r = new Random(); // ignore this for now
     
            lock( theWorld)
            {
                // The server is not required to send updates about every object,
                // so we update our local copy of the world only for the objects that
                // the server gave us an update for.
                foreach (Player play in players)
                {
                    //while (r.Next() % 1000 != 0) ; // ignore this loop for now
                    if (!play.Active)
                        theWorld.Players.Remove(play.ID);
                    else
                        theWorld.Players[play.ID] = play;
                }

                foreach (Powerup pow in powerups)
                {
                    if (!pow.Active)
                        theWorld.Powerups.Remove(pow.ID);
                    else
                        theWorld.Powerups[pow.ID] = pow;
                }
            }
     
      // Notify any listeners (the view) that a new game world has arrived from the server
      UpdateArrived?.Invoke();

      // TODO: for whatever user inputs happened during the last frame, process them.
    }

  }
}
