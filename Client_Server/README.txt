#Introduction 
This is a Snake Game project from CS3500 University of Utah 2022 Fall. 

#Describtion 
This program uses classic server-client architecture to pass data and practices MVC design principles. Users can play with others online by connecting to a same server. 

#Extra Feature

#Client Side
1.	Users can choose various themes, which have different maps with matching food icons BEFORE and During the game.
2.  Users can change the view size BEFORE and DURING the game.
2.	Users can use letter keys or number keys to send command to accommodate both desktop and laptop users. 
3.	Snakes are added with a cute smiley face with matching colors.
4.	If the attempt to connect fails BEFORE and During during the game, the users can reconnect and game will start over. (Given client can only reconnect if connection never worked, not after the game started.)
5.	Welcome page is added before users connect to server. 

/***********************   PS9   *****************************************/
#Server Side
1.	Users can switch to Extra Mode, where the food increases snake speed for 3 frames not the length.
2.	Users can set snake's initial length, speed and growth in settings file.
3.	Users can set Powerup respawn rate, max number and min number in the settings file.
4.	Dead snake would become a food at the head's position.
5.	Extra Mode: using the alternative powerup which boosts the speed up (x2) instead up growing
	and the snake who has the same color cannot collide each other.

Kind Reminder for Testing:
Since the extra mode has speed-up feature, it would appear to be laggin. especially when testing dicconnection handling with multiple AI. 
It is advised to test disconnection with "basic" mode.

#Important Design Decisions

#Client Side
1.	Project reference between View and Controller 
To avoid a dependency loop of project reference, we used project reference when the view is referring to the controller, 
while the controller invokes the view by using delegates. 

2.	Why the View have access to the Model?
For simplicity reason, the view has access to the World, but only reading it, not modifying it. 

3.	What is WorldPanel.IsConnected(sounds like related to the networking) for?
This enables the game to have a welcome page and only load game-related graphics when the user is successfully connected to the server. 

4.	Location for Invalidate()(may not be common mistake, but took us a while to fix)
Directedly called “Dispatcher.Dispatch(() => graphicsView.Invalidate());” in the GUI class instead of calling a wrapper method of this existing in the WorldPanel class. 
The reason is that the latter way would never hit the “Draw” method in WorldPanel.

5.	Why uses a lock?
Put a lock around the world object in the controller and the world panel when accessing its members. This is due to the nature of muti-threading programming. 
We need to ensure only one thread is working on the data. 

6.	How to use a lock?
We didn’t put a lock around every deserialization for each type of object of the world, even though they are done in different private helper methods. 
Instead, we put a lock around the whole process. This prevents the movement of the game to be flickering.

7.	Why need to check if the snake is moved?
When the fps is very low, the server would send multiple same information of a snake, is it not necessary to modify the snake collection in the world if it has not changed. 

8. Event loop optimization compared with the given one?
We used two event loop for handshake information and post-handshake information. This prevents the message process from being long and hard to read. 
It is a good practice of separation of concern. This also demosntrates our deep understanding of how the OnNetWork Delegate work in the Networking library. 

9.	How to process messages from the server?
Based on TCP and stream message work, we need first split each message by new line, and process each complete message. 
And delete each processed message from the buffer. The last message may not be complete, so we will keep it until its latter half arrives, 
and then process it (TCP guarantees messages will arrive and arrive in order). 

10.	Consideration for regex split
Regex split may result in empty string; we need to consider this situation. 

11.	Deserialization Optimization  
Used JObject and its query feature to detect what object we are getting from a Json string. This is less expensive than try and catch. 


/***********************   PS9   *****************************************/
#Server Side
1. How to read settings file and communicate with the Client?
We convert xml files to a GameSetting class and serialize the objects in this class to json string to send to the clients.
xml => c# object; c# object => json string.

2. How do we handle disconnection
First, we set the socket's player to disconnected state, and remove the socket from the server. When the server is updating the world,the disconnected player 
will be set to death state, because the healthy clients still need information to indicate this player is disconnected. 
And the healthy client would see the disconnected client have a death animation. 
Second, when the server sends message to all the clients, it would return a bool indicating this client is disconnected, and it will be removed from the server. 

3. How to  remove a data in a foreach loop
We don't. For example, when we are looping through the server's clients collection, we add disconnected ones to a new collection. 
After this foreach loop is done, we loop through the disconnected collection to remove them from the server's collection. 

4. How do we detect collsions
We used c# built-in library RectangleF to represent rectangles in the world. This is a well- tested class, so we don't need to worry about the math.

5. Separate of Concern
Server class doesn't directly modify any world objects or do any calculation. Its only job is to ask controller to read settings file, keeping the event loop to accepting connections,
and passing the processed string(command) to the controller.
Models are the classes handle heavy computations, such as collision detections.
Controller takes care of taking information from the server and updating the world(removing unwanted world objects).

6. How to optimize message sending 
We only send message at the end of each frame instead of in the middle of computaion, such as every time a new powerup is respawned. 

7. How to minimize program flickering?
We remove dead powerups from the world, but when new powerup comes in, the ID keeps incrementing instead of replacing the dead powerup's id. 
We also removes disconnected clients from the server, to speed up, especially when sending message is on the main thread. Similarly, the disconnected snakes will
be send to clients once and be removed from the world.

/**********************************************************************************/

#Room for Improvement 
#Client Side
1. Snake foods can be in various kinds in the same map to increase or decrese the score.


/***********************   PS9   *****************************************/
#Server Side
1. After connecting and disconnecting 10 AIs in the game for multiple times, the client would have minor flickering problem.

/**********************************************************************************/

Special thanks to the Professors and TAs help during the office hour and on piazza.
