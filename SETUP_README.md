# Stick Archers Battle - Setup Guide

## ✅ AUTOMATED (No Action Required)

The following are now automatically created at runtime:

| Feature | Status |
|---------|--------|
| Hit zone colliders | ✓ Auto-created on archers |
| Health bars UI | ✓ Auto-created on canvas |
| Round transition text | ✓ Auto-created |
| Headshot feedback UI | ✓ Auto-created |
| Wind indicator | ✓ Auto-created |
| Procedural arenas | ✓ Auto-generated (6 types) |
| 2P Mode button | ✓ Auto-created if missing |
| All managers | ✓ Auto-created (Audio, UI, etc.) |

---

## ⚠️ MINIMAL MANUAL STEPS

### Step 1: Create Tags & Layers (ONE TIME)

**Method A - Menu (Easiest):**
1. Open Unity
2. Click menu: `Tools → Setup Stick Archers Tags`
3. Done!

**Method B - Manual:**
1. Edit → Project Settings → Tags and Layers
2. **Tags** tab → Add: `Arena`
3. **Layers** tab → Add: `Ground` (any empty slot 8-31)

---

### Step 2: Create Prefabs (ONE TIME)

**Method A - Menu (Easiest):**
1. Click menu: `Tools → Validate Prefabs`
2. This creates placeholder prefabs automatically
3. Replace with your art later

**Method B - Manual:**
1. Create folder: `Assets/Prefabs/`
2. Drag your archer prefab → rename to `ArcherLocal.prefab`
3. Drag your arrow prefab → rename to `ArrowLocal.prefab`

**Method C - Use Placeholders:**
The game will auto-create stick figure placeholders if you don't have art yet.

---

### Step 3: Assign Prefabs (ONE TIME PER SCENE)

**Method A - Auto-assign:**
1. Open `GameArena` scene
2. Click menu: `Tools → Assign Prefabs to Bootstrap`

**Method B - Manual:**
1. Select `GameArenaBootstrap` object in scene
2. Inspector → Drag `ArcherLocal.prefab` to `archerLocalPrefab` slot
3. Inspector → Drag `ArrowLocal.prefab` to `arrowLocalPrefab` slot

---

### Step 4: Android Build (When Ready)

**Menu Method:**
1. Click menu: `Tools → Setup Android Build`

**Manual Method:**
1. File → Build Settings
2. Select `Android` platform
3. Click `Switch Platform`
4. Player Settings → Resolution → `Landscape Left`
5. Add scenes to `Scenes In Build`:
   - MainMenu
   - GameArena
6. Click `Build`

---

## 🎮 RUNNING THE GAME

### In Unity Editor:
1. Open `MainMenu` scene
2. Press **Play**
3. Everything auto-creates!

### Test Modes:
- **Practice**: 1P vs AI (test aim timing)
- **2 Players Local**: 2P on same device (split screen)
- **Online**: Photon multiplayer (if set up)

---

## 🔧 TROUBLESHOOTING

### "No Canvas found"
- Create: `GameObject → UI → Canvas`

### "Missing tag: Arena"
- Run: `Tools → Setup Stick Archers Tags`

### "Prefab not assigned"
- Run: `Tools → Validate Prefabs` then `Tools → Assign Prefabs to Bootstrap`

### Check all issues:
- Run: `Tools → Check Stick Archers Setup`

---

## 🎨 REPLACING PLACEHOLDER ART

After the game works with placeholders:

1. **Archer**: Replace `ArcherLocal.prefab` sprite with your stickman
2. **Arrow**: Replace `ArrowLocal.prefab` sprite with your arrow
3. **Arena**: Create arena prefabs or keep procedural generation

Keep the same component structure - just swap sprites!

---

## 📱 ANDROID BUILD CHECKLIST

- [ ] Platform switched to Android
- [ ] Orientation: Landscape
- [ ] Scenes added to build
- [ ] Minimum API Level: Android 5.0 (API 21)
- [ ] Build settings applied

---

## ✨ FEATURES IMPLEMENTED

✅ Auto-swaying aim (timing-based)  
✅ Split-screen 2P local mode  
✅ Hit zones (Head=100%, Body=30%, Limbs=15%)  
✅ Full 2D ragdoll physics  
✅ Procedural arenas (6 types)  
✅ Dynamic wind & gravity  
✅ Arrows stick in terrain  
✅ Headshot slow-motion effect  
✅ Round transitions  
✅ Health bars  
✅ Camera shake  
✅ Touch feedback  
✅ All auto-setup  

---

**Ready to play! Open MainMenu scene and press Play.**
