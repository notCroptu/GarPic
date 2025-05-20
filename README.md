# Sistemas de Redes para Jogos - Final Porject Report

## GarPic

## Student

- Mariana de Oliveira Martins, a22302203.

### Link to Repository

<https://github.com/notCroptu/GarPic>

### Link to Build

Link yuh

## **Report**

### Project Description

**GarPic** is a multiplayer mobile game where players take photos to visually represent secret words. In each round, all players receive the same word and must take a photo within a short time limit, if a player physically moves in the real world (with GPS), their timer is paused until they stop again, to encourage exploration.

Once all photos are submitted, players vote on which best represents the word and points are awarded based on votes, the number of rounds is based on the number of players, and a leaderboard is presented at the end of each session.

### Tech Decisions and Possibility Research

The project will use UnityNetcode for GameObjects to handle multiplayer communication, roles, points...

With a modular setup that supports LAN and Unity Relay hosting as taught in class to have private game sessions.

Creating this project for mobile would be the most feasible version of the game due to the need for free movement and use of a camera and GPS, but user given permission will also need to be given to the game and will be another to pic to explore later on.

#### Large Packets

To have a mobile version of the game, we would need a way to handle PNGs in the game.

To capture them is the simpler part, as we can use Unity's WebCamTexture to ask for camera feed. To send them from client to client however, we would need a way to handle large packets of data, for that the tool `UnityWebRequest` was found, used to upload images like the ones in the project to an external cloud, and then returning a link that can be passed to other clients using UnityNetcode for GameObjects.

[Unity Web Request](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)

#### Unity WebGL

Using WebGL was also considered, as it would people to play without requiring them to install anything, and while it would still need permission to access camera and gallery, it could potentially run on more platforms and devices.

However, capturing or selecting photos in a browser would require custom JavaScript to access the camera or gallery and convert the result into PNGs that could be uploaded using `UnityWebRequest`, and UnityNetcode for GameObjects cannot be used in `WebGL` builds, so multiplayer communication would need to rely on `WebSocket` (Unity plugin).

[WebSocket Unity Plugin](https://github.com/endel/NativeWebSocket)

Testing the browser version would also be more complicated, as it would require uploading to an external host (GitHub or itch.io) rather than testing directly in the Unity Editor or local builds.

While the accessibility of a no-install was great, the limitations were too great for that single benefit, and as a result, the Unity Mobile version was chosen as the more practical option for development.

### Server Functions

The game would need to keep track of:

- The room or lobby of players;
- The round’s correct word;
- The image submitted by each player;
- Player scores;
- Broadcast messages;
- Rounds (What things need to be shown, images, votes, photo taking period);
- GPS movement (is moving or not, no need for location).

### Conclusions

Wa

### **Bibliography**

1. [Title](link)
