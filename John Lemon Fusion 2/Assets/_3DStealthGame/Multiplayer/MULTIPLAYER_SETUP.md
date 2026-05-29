# John Lemon → Co-op Multiplayer (Photon Fusion 2)

This converts the single-player stealth game into a **co-op** game using **Fusion 2 Shared Mode**:
every player is a John Lemon trying to reach the exit together while avoiding the shared,
host-controlled ghosts/gargoyles.

> **Status of the SDK:** Fusion 2.0.12 is installed and a Fusion **App ID is already set**
> in `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`. No physics addon is required —
> the player uses Fusion's built-in `NetworkCharacterController`.

The C# is already written under `Assets/_3DStealthGame/Multiplayer/Scripts/`. Everything below
is the **Unity Editor wiring** you need to do (I can't click in the Editor for you).

---

## New scripts (what each does)

| Script | Replaces | Where it goes |
|---|---|---|
| `NetworkGameLauncher` | — | A scene "NetworkLauncher" object (with `NetworkRunner`) |
| `PlayerSpawner` | the hand-placed player | On the `GameManager` network object |
| `NetworkPlayerController` | `PlayerMovement` | On the networked **Player prefab** |
| `NetworkWaypointPatrol` | `WaypointPatrol` | On each enemy network object |
| `NetworkObserver` | `Observer` | On each enemy's vision trigger |
| `NetworkGameManager` | `GameEnding` | On the `GameManager` network object |
| `ExitZone` | `GameEnding`'s exit trigger | On the finish trigger object |
| `NetworkKey` | `Key` | On key prefabs/objects |
| `NetworkDoor` | `Door` | On door objects |

The original scripts are left untouched as reference — just don't put both versions on the same object.

---

## Step 1 — Make a networked Player prefab

1. Open `DemoScene` and duplicate `Demo_Player.prefab` → rename it **`NetworkPlayer`**
   (work on the duplicate so single-player stays intact).
2. On the **root** of `NetworkPlayer`:
   - **Add** `NetworkObject`.
   - **Remove** the `Rigidbody` (the original `PlayerMovement` used it). Fusion's CC drives motion.
   - **Add** `CharacterController` (Unity's built-in). Size its height/radius to the model
     (≈ height 1.6, radius 0.25, center y ≈ 0.8 for John Lemon). The old `CapsuleCollider`
     can be removed — the `CharacterController` is the collider now.
   - **Add** `NetworkCharacterController`. Set `Max Speed` to the old `walkSpeed` (≈ 1),
     `Rotation Speed` ≈ 15. Gravity can stay default.
   - **Add** `NetworkMecanimAnimator` and drag the player's `Animator` into its `Animator` field.
     (This is what syncs the `IsWalking` animation to other players.)
   - **Remove** the old `PlayerMovement` component and **Add** `NetworkPlayerController`.
3. Wire `NetworkPlayerController`:
   - **Move Action** → set the same WASD/Arrows binding the old component used
     (`Vector2`, Up/Down/Left/Right). You can copy it from the original prefab's `PlayerMovement`.
   - **Local Only Objects** → see Step 2 (camera).
   - Keep the existing `AudioSource` (footsteps) on the root — the controller finds it.
4. Make sure pickups can be detected: the `CharacterController` fires `OnTriggerEnter` against
   trigger colliders, so keys/doors just need trigger colliders (Step 5).

### Register the prefab with Fusion
Fusion auto-detects prefabs that have a `NetworkObject`. After adding the `NetworkObject`,
open **Tools ▸ Fusion ▸ Rebuild Object Table** (or just enter Play once) so `NetworkPlayer`
is registered in `NetworkProjectConfig`.

---

## Step 2 — Camera & AudioListener (per-player)

In single-player there's one camera + one AudioListener. In co-op each client must see/hear
through **its own** player only.

**Recommended simple approach — make the camera a child of the player:**
1. Move the camera rig (the `Demo_PointOfView` / Cinemachine camera + the `Camera` with the
   `AudioListener`) so it is a **child of the `NetworkPlayer` prefab**, positioned at the
   usual top-down offset.
2. Drag that camera rig GameObject into `NetworkPlayerController ▸ Local Only Objects`.
   - Remote players' camera objects get disabled automatically in `Spawned()`.
   - The `AudioListener` (on the player root) is also disabled for remote players in code.

> Prefer to keep one shared Cinemachine camera in the scene instead? Then leave the camera out
> of the prefab and, in `NetworkPlayerController.Spawned()`, set the scene camera's
> `Follow`/`LookAt` to `transform` when `HasStateAuthority`. The child-camera approach above is
> simpler and has no Cinemachine dependency.

---

## Step 3 — Launcher (connection)

1. Create an empty GameObject in `DemoScene` named **`NetworkLauncher`**.
2. Add `NetworkRunner` and `NetworkGameLauncher` (it auto-adds a `NetworkSceneManagerDefault`).
3. On `NetworkGameLauncher`: set `Session Name` (any string; players sharing it meet in the
   same game), `Max Players` (e.g. 4). Leave `Auto Connect` off to use the test GUI, or turn it
   on to connect immediately on Play.

When you press Play you'll get a tiny **Host / Join** button (top-left). Click it to start a
Shared session. Replace this with the existing UI Toolkit `MainMenu` later by calling
`launcher.Connect()` from the Start button.

---

## Step 4 — GameManager + PlayerSpawner

1. Create an empty GameObject **`GameManager`** in `DemoScene`.
2. Add a `NetworkObject` to it (check **Is Master Client Object** is fine; scene objects are
   owned by the Master Client in Shared Mode).
3. Add `NetworkGameManager`:
   - `UI Document` → the same `UIDocument` the old `GameEnding` used (with `EndScreen`,
     `CaughtScreen`, `Demo_TimerLabel`).
   - `Exit Audio` / `Caught Audio` → the same AudioSources as before.
4. Add `PlayerSpawner` to the same object:
   - `Player Prefab` → drag the **`NetworkPlayer`** prefab.
   - `Spawn Points` → drag a few empty Transforms placed at the start room (one per player so
     they don't stack). If left empty, everyone spawns at the GameManager's position.
5. Delete (or disable) the original hand-placed `Demo_Player` and the old `GameEnding` object
   from the scene — players are now spawned at runtime.

---

## Step 5 — Keys & Doors

For **each** key object in the scene/level prefabs:
1. Add a `NetworkObject`.
2. Replace `Key` with `NetworkKey`; set `Key Name` to match the old value.
3. Ensure it has a **trigger** `Collider`.

For **each** door:
1. Add a `NetworkObject`.
2. Replace `Door` with `NetworkDoor`; set `Key Name`.
3. Add a **trigger** collider (a child trigger is fine) so the player's CharacterController
   detects it. Keep the solid collider too if the door should block until opened.

> Keys/doors that live inside the **Level prefabs** must also be registered: after editing,
> run **Tools ▸ Fusion ▸ Rebuild Object Table**. Keys/doors placed directly in the scene are
> picked up as scene network objects automatically.

---

## Step 6 — Enemies (ghosts / gargoyles)

For each patrolling enemy (e.g. `Demo_Ghost`) and each watcher (`Demo_Gargoyle`):
1. Add a `NetworkObject` to the enemy root.
2. Add a `NetworkTransform` (so its movement replicates to clients).
3. Replace `WaypointPatrol` with `NetworkWaypointPatrol`; re-assign `waypoints` + `moveSpeed`.
   - Remove the enemy's `Rigidbody` if it had one — `NetworkWaypointPatrol` moves the transform
     directly and `NetworkTransform` syncs it.
4. On the enemy's vision trigger, replace `Observer` with `NetworkObserver`:
   - `Game Manager` → drag the scene `GameManager`.
   - Keep its trigger `Collider`.

Enemies are scene network objects, so the **Master Client** simulates them and everyone else
sees the synced result.

---

## Step 7 — Build Settings & test

1. **File ▸ Build Settings**: make sure `DemoScene` (and `MainMenu` if you use it) are in the
   Scenes list.
2. **Quick 2-player test without two machines** — use Fusion's Multipeer:
   - `NetworkProjectConfig` ▸ **Peer Mode = Multiple**.
   - Add the **`RunnerVisibility`** / or use Fusion's *Multi-Peer* so two players run in one
     editor, OR simpler: make a **build** and run the build + the Editor at the same time, click
     Host/Join on both with the **same Session Name**.
3. Press Play (or launch the build), click **Host / Join** on each instance. You should see two
   John Lemons moving independently, shared enemies, shared keys/doors, and a shared win/lose.

---

## Co-op rules implemented (tweak freely)

- **Lose:** if *any* player is seen by an enemy → everyone loses (`NetworkGameManager.ReportPlayerCaught`).
- **Win:** when *all* active players have reached the exit (`ReportPlayerAtExit`).
- **Keys:** picked up per-player (each John Lemon carries their own keys).
- On end, the Master Client reloads the gameplay scene for everyone.

To change "win when all reach exit" → "win when any reaches exit", edit the comparison in
`NetworkGameManager.ReportPlayerAtExit`. To make caught only remove that one player instead of
ending the game, change `ReportPlayerCaught`.

---

## Troubleshooting

- **"NetworkObject … is not registered"** → run **Tools ▸ Fusion ▸ Rebuild Object Table**.
- **Player spawns at world origin** → assign `Spawn Points`, and make sure the player prefab has
  `NetworkCharacterController` (its `Spawned()` fixes the initial position).
- **Can't connect** → confirm the App ID in `PhotonAppSettings` and that both instances use the
  **same Session Name** and the same app version/build.
- **Two cameras / doubled audio** → the camera rig isn't in `Local Only Objects`, or the
  `AudioListener` isn't a child of the player. See Step 2.
- **Enemies don't move on clients** → missing `NetworkTransform` on the enemy.
