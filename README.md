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

However, capturing or selecting photos in a browser would require custom JavaScript to access the camera or gallery and convert the result into PNGs that could be uploaded using `UnityWebRequest`, and `UnityNetcode` for GameObjects cannot be used in `WebGL` builds, so multiplayer communication would need to rely on `WebSocket (Unity plugin).

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

[![How to setup Unity Remote 5](https://img.youtube.com/vi/L-48i5VclSc/0.jpg)](https://www.youtube.com/watch?v=L-48i5VclSc)

But otherwise the exact requirements would be:

- On an Android device running on Android 5.0 or later
  - **Unity Remote 5** app installed from the [Google Play Store](https://play.google.com/store/apps/details?id=com.unity3d.mobileremote)
  - **Developer Mode** enabled:
    - Go to *Settings > About phone* and tap the Build number seven times ( also MIUI number or OS number)
  - **USB Debugging** enabled:
    - Go to *Settings > System > Developer options* and enable *USB debugging*
  - **USB Data Transfer** enabled:
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

> **Note:** It works the best with a strong internet connection.

After this, I substituted the build and launch logic in `NetworkSetup` to use the .bad for launch on any connected phone.

#### Debugging

To debug builds on mobile, I just needed to once again use adb to start a `logcat` while the phone is connected with udb debugging like described here:

[Unity Discussions - How to use adb logcat?](https://discussions.unity.com/t/how-to-use-adb-logcat/75785/2)

The command for cmd would look like this:

`"C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" logcat -s Unity`

### Permissions

An example oif permissions necessary for the project would be the counter that is stopped when the player is moving, for which we need GPS, which is accessible using Unity's Location Services, for which the (Old) Input system already provides a variable of `Input.location`.

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

The .bat file for build was very helpful in testing the previous process, as when testing with Unity Remote, the device does stream GPS, but the Editor’s own permissions can cause `Input.location.isEnabledByUser` to return false even when location is enabled on the host device.

[Stack Overflow - Input.LocationService.isEnabledByUser returning false with Unity Remote in the Editor](https://stackoverflow.com/questions/45340418/input-locationservice-isenabledbyuser-returning-false-with-unity-remote-in-the-e)

For that reason we must build on android and to make sure permissions for location are enabled.

Although it is also necessary to ask for permissions for the Camera, as both GPS and Camera permissions are considered dangerous, it is a lot less hazardous as you don't need to verify any camera services, so the only thing we must do is call `Application.RequestUserAuthorization` at runtime. The documentation came with the correct implementation but I chose follow the one I already had:

[Unity Documentation - WebCamTexture](https://docs.unity3d.com/ScriptReference/WebCamTexture.html)

### UnityWebRequest

Unity Web Request proved to be useful for more than sending camera data, as I wanted players to get unexpected words rather than ones I preset from my own list so I explored using online APIs for which I found UnityWebRequest to be useful.

#### Random Word API

My first attempt used [Random Word API](https://random-word-api.herokuapp.com/word), as it was very simple to integrate and returned a new word every time. However, the words were often too technical, too weird even for the unexpectedness I was hoping for, and worried it would end up messing up the experience (like rare scientific names).

So looking for more control, I discovered the [Datamuse API](https://www.datamuse.com/api/), which lets you specify restrictions in the URL so you can even find words that fit certain meaning, spelling, sound, vocabulary and even theme. This also opened up the possibility to later let me give players the liberty to choose their own theme to bias word selection towards what they were feeling like.

To integrate this into Unity, more specifically in the `WordSelection` script, I first created a list of topics, that would be search using the APIs `topics` query, though I later learned not all words in this query yield results, and had to move to a more stable query, though less appropriate `rel_jja`, that searches words relating to a given noun, essentially it results in more descriptive words based on that noun, instead of directly relating to a given topic.

> **Note:** This might not have been a good API to use later on, as it has a maximum of 100,000 requests per day. (For example, with an average of 8 rounds per session and approximately 80,000 sessions per day (based on Gartic statistics during the pandemic), you could need up to 640,000 requests per day, plus, multiple requests might be needed per round if Word Selection fails.)

#### Data Retrieval

With a list of topics set, I used `UnityWebRequest` to call a custom Datamuse url from a random topic, which was easy to learn as all the code needed was exemplified in the *OLD* official page [official documentation](https://docs.unity3d.com/540/Documentation/Manual/UnityWebRequest).html.

Basically, you create a new `UnityWebRequest`, input a source URL from which it will get data (`UnityWebRequest www = UnityWebRequest.Get("url"`) and wait for it to return a result inside a coroutine, where upon completion you can check for success and choose to try again.

The coroutine part worked well with my setup as I had already decided to have a Game Loop based on coroutines.

If successful, the `UnityWebRequest` handler will contain any data retrieved from the webpage (in my case, only the text, which is JSON as most word APIs return), and I then only needed to parse the returned JSON response to extract the first word.

#### Data Sending

For camera implementation with `UnityWebRequest`, I needed my own database solution for storing images. I chose Supabase, recommended by a friend for being more affordable , performant, and for offering authentication support.

The implementation for sending images using `UnityWebRequest` was similar to sending packets in retrieving json data, but this time I had to choose between HTTP `PUT` and `POST` for uploading. I chose `PUT` because it is used to upload or replace a resource at a specific path and if the file does not exist, it creates a new one, and if it already exists, it overwrites it. `POST` is used for creating a new resource without specifying the path, which was not ideal for storing session and player specific data by their IDs.

[Supabase - http: RESTful Client](https://supabase.com/docs/guides/database/extensions/http)

When setting up Supabase, I created a storage bucket and was provided with the bucket URL, an anonymous key, and the bucket name, which are necessary to authenticate upload requests from the Unity client to Supabase storage.

The variables I created (`supabaseURL`, `supabaseANON`, `supabaseBUCKET`) are intended for the use of the client specifically, as connecting the client to the database (with a *connection string*) would compromise the database's security as it would give players my database credentials such as the password.

For `UnityWebRequest` to be able to handle the images through the clients without needing a deeper user authentication, I had to include policies to my Supabase bucket to allow anonymous access with the keys above for insert (upload) and select (read) operations in the bucket only.

This doesn't mean it will take more work later as I only need clients to be aware of the correct URL for uploads and downloads and use `UnityWebRequest` both for sending images and retrieving them later, so I don't need to have my own backend for this structure and therefore no need to use *connection strings*.

The client credentials however had to be put away in a json, hidden by GitIgnore in order to keep the project safe in the git. Therefore Supabase would only work in my own PC or builds I make.

### Server Relay

To make playing sessions similar to popular party games, with a session code, GarPic uses Unity Relay Servers using `UnityNetcode for GameObjects` learned through the implementation taught in class:

[![Diogo Andrade - Sistemas de Redes para Jogos - Aula 19/05/2025](https://www.youtube.com/watch?v=Ql9hg1mvBRM&list=PLheBz0T_uVP3JaTA4wMs38MgOKiIpDpLG)]

Relay servers, means that rather than hosting game worlds themselves, relay servers only provide the connections between players, which means they can serve many sessions at once, some relay servers like Steam's and PSN provide methods of authentication but we will only need anonymous users.

This approach came from the setup simplicity for players, a simple UI and private sessions for friends that are easily manageable with short codes using Cloud.Unity relay, as normal peer-to-peer networking suffers from firewall issues, and Unity Relay, like other relay services, solves this by keeping IPs between users safe:

[Unity Support - About Relay IPs, Ports and Firewall Considerations](https://support.unity.com/hc/en-us/articles/20152286417044-About-Relay-IPs-Ports-and-Firewall-Considerations)

#### Goals

On opening the app, players would be met with a join button, to join existing available sessions through a code, or a host button to create a new session. Sessions would have a max of 8 players, but players would be allowed to join at any time, keeping in mind the game would not be prolonged for their benefit.

When starting the game, players can either host a new session (generating a unique code) or join an existing session using a code shared by the host, sessions should handle up to 8 players and are open for joining until the game ends, but new players will join the ongoing game rather than restarting it for everyone.

#### Cloud.Unity

Firstly, I had to add the Relay service provided by Unity to the project through Unity Dashboard, which was easy, and the project was already registered in my account there.

Then, on editor I turned on Relay on `UnityTransport` and used the `NetworkSetup` script to save transport information such as if the client is server, and if it is hosted as relay, and what the session number is, which will be helpful for identifying photos in Supabase.

#### Server Setup

When entering the game, players have the option to host a session, which creates a new relay server where they can play.

Relay setup needs to be async, contrary to usual network workings, and had to be checked through a Task called Login, where an exception is run in case the Unity authentication services are not able to initialize and sign in.

Then another Task, `CreateAllocationData`, calls the service again to create a new session connection with a given max players (8 for GarPic), which I can then ask for session information, like the session code, and store it in the class `RelayHostData`.

Using one of the settings of the `RelayHostData` object, the allocation ID, I can call a final task `GetJoinCodeAsync`, to retrieve the session code for players to join with.

After this the player is sent into the game scene where they may wait for at least 3 players to join, after which the host can start the game, and then the game logic is initialized and all clients synchronize to the main game scene.

#### Client Setup

Players who want to join a an existing session act as clients connect through the Unity Relay server using the session code provided by the host.

Upon inserting this code one the Join button on teh lobby, the code is sent to the Relay service to find and join the correct allocation through the `JoinAllocationAsync` task.

Once connected, the client’s state the game state is ready to be synchronized with the host.

#### Error Handling

Errors are displayed to the viewer at runtime in case a connection is not established.

### Netcode

Since I didn’t need to synchronize transforms, I decided to use a single `NetworkObject` object that contains both the session manager and game loop logic, and serves as the central authority for all network communication.

Since I didn’t need to synchronize any transforms, I thought this choice was ideal, with the goal of reducing relay traffic by consolidating all network communication into one object instead of spreading it across multiple GameObjects with separate `NetworkObject` components, even if those had transform syncing disabled.

To start, I added the `NetworkObject` cat the top of my component list, as the unity documentation warns to do for proper execution order:

[Unity Netcode Docs - NetworkObject Component Order](https://docs-multiplayer.unity3d.com/netcode/current/basics/networkobject/#component-order)

#### Session Lobby

The session lobby is managed by the `SessionStart` script. At first, all players are displayed using their `clientId`, but each player can edit their nickname, which is then used throughout the game session.

Nicknames are shared across all players using a `NetworkList`, which is owned and modified only by the host. Clients cannot modify the list directly but must send a request to the host instead containing the nick string and their client ID.

Since I couldn’t rely on each client to detect new connections and add entries themselves as they could add each other multiple times, I created a server RPC method, `SetNicknameServerRpc`, which updates the nickname associated with a given `clientId` when requested. Each client has a confirm button tied to `UpdatePlayerNickname`, which sends this request to the server. Once the nickname is updated in the list, the `OnListChanged` event is triggered, which all clients listen to in order to refresh their UI.

At first, I had issues with the nickname list not being initialized properly. Even though I was assigning it in `Awake()`, I kept getting null reference errors and `OnListChanged` was not firing. After some research and testing, I realized that this was happening because `Start()` was running before the `NetworkObject` was fully initialized, and as a result of initializing the list before `NetworkObject` it became null again when it finally searched for network variables to sync.

To fix this, I moved all network-related logic into one of `NetworkBehavior`s methods, `OnNetworkSpawn()`, that is guaranteed to run only after the network object is setup.

I found this behavior explained and confirmed in this video:

[![Multiplayer Setup in Unity NGO - OnNetworkSpawn](https://youtu.be/YmUnXsOp_t0?si=Rhw4YgKmYYqXap2E&t=4587)]

[![Multiplayer Setup in Unity NGO - NetworkVariable](https://youtu.be/YmUnXsOp_t0?si=38MoRjY6DzF_RD0V&t=6374)]

However, even after this, changes to the nickname list were still not being shared with other clients. I found the problem was that `SessionStart` was placed directly in the scene and not set to spawn like with dynamic network objects using `NetworkObject.Spawn()`, and because I was also switching scenes (from the lobby to the game), the object was not automatically registered and synced across clients.

[Unity Forum - Scene object with NetworkObject disappears on client](https://discussions.unity.com/t/scene-object-with-networkobject-disappears-on-client/939204)

So I enabled Scene Management in the `NetworkManager` and started using `NetworkManager.Singleton.SceneManager.LoadScene()` to transition between scenes instead of directly with `SceneManager`, so it knows to find and synchronize scene spawned objects across all clients during scene loading.

### Conclusions

DontDestroyWithOwner for switching host in case of disconnect?

events and delegates are very good

Wa

https://www.youtube.com/watch?v=YmUnXsOp_t0
https://discussions.unity.com/t/how-to-use-networklist/947471/2

https://discussions.unity.com/t/how-to-refer-to-internal-json-file-on-android/222832

https://discussions.unity.com/t/how-do-i-make-locationservice-start-work/153995

https://docs.unity3d.com/Manual/UnityRemote5.html
https://stackoverflow.com/questions/15800303/pausing-an-assembly-program
https://developer.android.com/tools/adb
https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html
https://docs.unity3d.com/Manual//android-manifest.html

### **Bibliography**

1. [Title](link)
