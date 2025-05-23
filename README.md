# Sistemas de Redes para Jogos - Final Project Report

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

The project will use `UnityNetcode` for GameObjects to handle multiplayer communication, roles, points...

With a modular setup that supports LAN and Unity Relay hosting as taught in class to have private game sessions.

Creating this project for mobile would be the most feasible version of the game due to the need for free movement and use of a camera and GPS, but user given permission will also need to be given to the game and will be something else to explore later on.

#### Large Packets

To have a mobile version of the game, we would need a way to handle PNGs in the game.

To capture them is the simpler part, as we can use Unity's WebCamTexture to ask for camera feed. To send them from client to client however, we would need a way to handle large packets of data, for that the tool `UnityWebRequest` was found, used to upload images like the ones in the project to an external cloud, and then returning a link that can be passed to other clients using UnityNetcode for GameObjects.

[Unity Web Request](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)

#### Unity WebGL

Using WebGL was also considered, as it would people to play without requiring them to install anything, and while it would still need permission to access camera and gallery, it could potentially run on more platforms and devices.

However, capturing or selecting photos in a browser would require custom JavaScript to access the camera or gallery and convert the result into PNGs that could be uploaded using `UnityWebRequest`, and `UnityNetcode` for GameObjects cannot be used in `WebGL` builds, so multiplayer communication would need to rely on `WebSocket` (Unity plugin).

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
- GPS movement (is moving or not, no need for location to be passed).

### Mobile Testing

For testing and debugging mobile input and gameplay during development, I first installed Unity Remote 5 on an Android device.

This video should explain the process:

[![How to setup Unity Remote 5](https://www.youtube.com/watch?v=L-48i5VclSc)]

But otherwise the exact requirements would be:

- On an Android device running on Android 5.0 or later
  - **Unity Remote 5** app installed from the [Google Play Store](https://play.google.com/store/apps/details?id=com.unity3d.mobileremote)
  - **Developer Mode** enabled:
    - Go to *Settings > About phone* and tap the Build number seven times ( also MIUI number or OS number)
  - **USB Debugging** enabled:
    - Go to *Settings > System > Developer options* and enable *USB debugging*
  - USB cable capable of data transfer
  - These **Unity Modules**:
    - Android Build Support
    - Android SDK & NDK Tools
    - OpenJDK

In Unity, in Editor Project Settings it would also be necessary for Device to be set to *"Any Android Device"*.

After this, on connecting the Android phone via USB to the computer running the Unity Editor allows the game view to stream directly to the device using Unity Remote 5.

#### Build, Install and Launch (.bat)

However, this setup only serves a way to debug input and rendering, and wouldn't reflect the device performance since it's still running from the editor, and some device inputs might still need to be built onto a mobile phone to reliable test them.

[Unity Remote 5](https://docs.unity3d.com/Manual/UnityRemote5.html)

For that reason, and to later use for testing in several phones at the same time, I had to create a Windows batch (.bat) script that would, after building, allow me to send an APK, install it, and run it on all USB Debugging connected devices to the PC.

ADB (Android Debug Bridge) came with the Unity Android Modules, though the adb short cut was not working even after inserting it into the PC Environment Variables, so for the script to work, one would need to install the unity module for the version used in the project (6000.0.30f1) or install ADB some other way and correct the path to adb.exe inside the .bat file.

[Android Debug Bridge](https://developer.android.com/tools/adb#install)

A recurring problem was the automatization of this for multiple devices at the same time, as when there are multiple devices connected we need to specify which one to apply commands to, or it may cause `adb.exe: more than one device/emulator`.

This and the fact that batch does not include loop breaks or threading and GOTOs are tricky in loops and made the recessivity a bit hard to handle.

The script logic goes as follows:

- The script first uses `adb devices` to list all connected devices (USB).
- Loops through each found serial number.
- Runs connect/install/launch commands on specified targets.
- Disconnects all previous connections for cleanup.

### GPS and Camera Permissions

For the counter that is stopped when the player is moving, GPS is necessary, which is accessible using Unity's Location Services, for which the (Old) Input system already provides a variable of `Input.location`.

[Unity Documentation - LocationService](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/LocationService.html)

Even then the Input.location variable needed to be set up and for that I had to search how to correctly use it, as some features ike permissions vary between devices.

Even with this variable, setup is required, which can vary significantly between devices and platforms. I referenced this Medium article to correctly initialize GPS at start, using coroutines:

[Medium - How to Access GPS Location in Unity](https://nosuchstudio.medium.com/how-to-access-gps-location-in-unity-521f1371a7e3)

Location Services operate asynchronously, so it’s important to run them in parallel with the main thread, and to update the GPS status whenever the player changes permissions.

During testing on different devices, I noticed inconsistencies with GPS activation and considered creating a custom Android Manifest before building, to make sure permissions were required at before even launching. However, while it is possible to achieve this by placing the manifest in the correct folder hierarchy and setting it up to match build preferences, managing the manifest file can be complex and may cause the app to become uninstallable on some devices as I learned, because of the dynamic nature of Unity builds (that build the Manifest according to script necessities), led me to prefer runtime permission requests for now.

[Android App Manifest Documentation](https://docs.unity3d.com/Manual//android-manifest.html)

On Android it's supposed to request location permission on app launch, and wait until it is granted to add the stop timer feature.

If enabled, the game then periodically checks for GPS movement by comparing longitude and latitude of the GPS last placements, stopping the timer if there is a significant delta.

Though this location may not always be correctly reflected by the phone, and may cause jitters that falsy affect this delta, so for this we would need to test different values of Accuracy In Meters and Distance In Meters inserted at `Input.location.Start(x, y)` for better accuracy.

The .bat file for build was very helpful in testing GPS, as when testing with Unity Remote, the device does stream GPS, but the Editor’s own permissions can cause `Input.location.isEnabledByUser` to return false even when location is enabled on the host device.

[Stack Overflow - Input.LocationService.isEnabledByUser returning false with Unity Remote in the Editor](https://stackoverflow.com/questions/45340418/input-locationservice-isenabledbyuser-returning-false-with-unity-remote-in-the-e)

For that reason we must build on android and to make sure permissions for location are requested and enabled.

### Conclusions

Wa

https://discussions.unity.com/t/how-do-i-make-locationservice-start-work/153995

https://docs.unity3d.com/Manual/UnityRemote5.html
https://stackoverflow.com/questions/15800303/pausing-an-assembly-program
https://developer.android.com/tools/adb
https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html
https://docs.unity3d.com/Manual//android-manifest.html

### **Bibliography**

1. [Title](link)
