# Stick Archers Battle — Android (Online Multiplayer)

> **Note:** This guide is legacy. For current setup, use **[SETUP_README.md](SETUP_README.md)**.
> Architecture and gameplay docs: **[Documentation/](Documentation/)**.

## Step 1: Install Unity

1. Download **Unity Hub** from https://unity.com/download
2. Inside Unity Hub, install **Unity 2022 LTS**
3. During install, check: **Android Build Support** + **Android SDK & NDK Tools**

## Step 2: Open the Project

1. In Unity Hub → Open → select this folder (`stick-archer/`)
2. Unity will import all scripts automatically

## Step 3: Install Photon PUN 2

1. In Unity → Window → Asset Store → search **"Photon PUN 2 - FREE"**
2. Import it into the project
3. Go to **Window → Photon Unity Networking → PUN Wizard**
4. Enter your free Photon App ID (get one at https://www.photonengine.com → create app → Realtime)

## Step 4: Add Free Art Assets

- Download stickman sprites from https://kenney.nl/assets/toon-characters-1
- Import PNG files into `Assets/Art/Sprites/`
- Create Animator Controller for Archer with states: Idle, Charging, Ragdoll

## Step 5: Set Up Scenes

### MainMenu scene
- Create Canvas with Panels for: Main Menu, Lobby, Result, Opponent Left
- Attach `UIManager.cs` to a GameObject named "UIManager"
- Attach `NetworkManager.cs` to a GameObject named "NetworkManager" (mark DontDestroyOnLoad)
- Wire up all UI text/button references in Inspector

### GameArena scene
- Add a flat platform (Sprite + BoxCollider2D) as the ground
- Add two empty GameObjects as spawn points (Player1Spawn, Player2Spawn)
- Add `ArenaManager.cs` to a root object (needs PhotonView component too)
- Create Prefabs folder and add:
  - `Archer.prefab` — Stickman sprite + Rigidbody2D + Archer.cs + PhotonView + PhotonTransformView
  - `Arrow.prefab` — Arrow sprite + Rigidbody2D + Arrow.cs + PhotonView + CircleCollider2D (IsTrigger=true)
- Register both prefabs in `Resources/` folder (Photon requires this for network instantiation)

### Touch Controls (GameArena Canvas)
- Left half: Virtual Joystick background image → attach `TouchControls.cs` (isFireButton = false)
  - Child image = joystick handle → assign to `joystickHandle`
- Right half: Fire button image → attach `TouchControls.cs` (isFireButton = true)

## Step 6: Build for Android

1. File → Build Settings → switch platform to Android
2. Player Settings → Company Name, Package Name (e.g. com.yourname.stickarchers)
3. Minimum API Level: Android 7.0 (API 24)
4. Add both scenes to Build (MainMenu index 0, GameArena index 1)
5. Click Build → save as `StickArchers.apk`
6. Install on Android: `adb install StickArchers.apk`

## Script Overview

| Script | Purpose |
|--------|---------|
| `GameManager.cs` | Score tracking, win condition, round reset |
| `Archer.cs` | Aim angle, charge mechanic, arrow firing, ragdoll |
| `Arrow.cs` | Physics launch, rotation, hit detection, network sync |
| `NetworkManager.cs` | Photon connection, matchmaking, player spawn |
| `UIManager.cs` | All screen transitions and HUD updates |
| `TouchControls.cs` | Virtual joystick + fire button for Android |
| `ArenaManager.cs` | Random arena selection synced across network |
