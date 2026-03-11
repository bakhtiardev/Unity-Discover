# Transferring MRBike to Another Unity Project

This guide explains how to transfer the MRBike experience — including its 3D models, animations, interactions, audio, and haptic feedback — into a separate Unity project targeting Meta Quest.

## Overview

MRBike is a self-contained Mixed Reality experience located at [`Assets/MRBike/`](../Assets/MRBike). Its root is the [`BikeInteraction` prefab](../Assets/MRBike/Prefabs/BikeInteraction.prefab), which orchestrates the entire bike assembly workflow. The experience depends on a small set of packages that must also be present in the destination project.

---

## Prerequisites

Ensure your destination project meets the following requirements before copying any assets.

- **Unity** 6000.0.59f2 or newer
- **Target platform**: Meta Quest (Android, ARM64)
- **Rendering**: Universal Render Pipeline (URP) — required by the MRBike shaders

---

## Step 1 — Install Required Packages

MRBike scripts depend on the following packages. Add them to your destination project before importing any assets.

### Meta XR SDK (via Unity Package Manager)

Open **Window > Package Manager**, switch the source to **My Registries** (scoped registry for `npm.developer.oculus.com`), then install:

| Package | Purpose |
|---|---|
| `com.meta.xr.sdk.core` | `OVRInput`, haptic vibration API |
| `com.meta.xr.sdk.interaction.ovr` | Interaction SDK — grabbable objects, hand-grab interactables |
| `com.meta.xr.sdk.audio` | Meta XR Audio spatializer used by bike audio sources |

> All three packages are available from the [Meta XR All-in-One SDK](https://developers.meta.com/horizon/documentation/unity/unity-package-manager/) registry entry or individually through the Meta Developer Hub.

### Photon Fusion

MRBike uses Photon Fusion for multiplayer state synchronization (`NetworkBehaviour`, `[Networked]`, `[Rpc]`). Download and import **Photon Fusion 1** from the [Photon SDK download page](https://doc.photonengine.com/fusion/current/getting-started/sdk-download).

> **Single-player alternative**: If you do not need multiplayer, see [Appendix A](#appendix-a--removing-photon-fusion-dependency) for guidance on replacing the Photon Fusion network behaviours with local-only equivalents.

### TextMeshPro

TextMeshPro is bundled with Unity. Import the **TMP Essential Resources** via **Window > TextMeshPro > Import TMP Essential Resources** if you have not already done so.

### Meta Utilities Package

Several MRBike scripts use attributes (`[AutoSet]`, `[AutoSetFromParent]`, `[Capacity]`) and helpers from the `com.meta.utilities` package included with this repository at [`Packages/com.meta.utilities/`](../Packages/com.meta.utilities/).

Add it to the destination project using its Git URL:

1. Open **Window > Package Manager**.
2. Click the **+** button and choose **Add package from git URL**.
3. Enter:
   ```
   https://github.com/oculus-samples/Unity-Discover.git?path=Packages/com.meta.utilities
   ```

Alternatively, copy the entire `Packages/com.meta.utilities/` folder into the destination project's `Packages/` directory.

---

## Step 2 — Copy the MRBike Assets

Copy the following folder in its entirety into the destination project's `Assets/` directory:

```
Assets/MRBike/
├── Animations/       # Animation clips and Animator controllers
├── Audio/            # Sound effects (SFX/) and voice-overs (VO/)
├── Fonts/            # UI fonts
├── Materials/        # Bike, tool, and UI materials
├── Models/           # Bike, tool, and UI 3D models (FBX)
├── Prefabs/          # All prefabs, including BikeInteraction.prefab
├── Scripts/          # All C# scripts (MRBike namespace)
├── Shaders/          # Custom shaders (ContactShadow, Lighting, LightingAffordance)
└── Textures/         # Bike, tool, and UI textures
```

> **Git LFS**: The repository uses Git LFS for binary assets (models, textures, audio). Ensure Git LFS is installed (`git lfs install`) before cloning or pulling so that binary files are downloaded correctly.

---

## Step 3 — Copy the Haptics System (Optional but Recommended)

MRBike's interactive grabbable objects trigger haptic feedback through the Meta XR Interaction SDK's built-in haptic affordances. This works automatically once the Interaction SDK is installed.

If you also want the project-level `HapticsManager` singleton (which lets any script queue vibrations by force level), copy the following file:

```
Assets/Discover/Scripts/Haptics/HapticsManager.cs
```

Place it anywhere in your project's script directories and change the namespace from `Discover.Haptics` to match your project's conventions.

Then place the `HapticsManager` prefab in your scene, or add it to your project's bootstrap/initialization logic. The manager uses `DontDestroyOnLoad` and exposes:

```csharp
// Vibrate from the main thread
HapticsManager.Instance.VibrateForDuration(VibrationForce.MEDIUM, 0.15f, OVRInput.Controller.RTouch);

// Queue vibration from a background thread
HapticsManager.Instance.QueueVibrateForDuration(VibrationForce.LIGHT, 0.1f, OVRInput.Controller.LTouch);
```

---

## Step 4 — Set Up the Scene

### 4a. OVR Camera Rig

Add a properly configured **OVR Camera Rig** to your scene. Enable **Hand Tracking Support** and **Controller Support** in the `OVRManager` component (or in the **Meta XR > Project Setup Tool**).

### 4b. Interaction SDK

Add the **Comprehensive Interaction Prefab** (or the equivalent setup for your project) to your scene. This sets up the Interaction SDK rig required by all grab interactables in the BikeInteraction prefab.

### 4c. Photon Fusion Runner

If you are using multiplayer, add a `NetworkRunner` to your scene and configure your **Photon App ID** in `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`. See the [Configuration guide](Configuration.md) for detailed Photon setup instructions.

### 4d. Place the BikeInteraction Prefab

Drag [`Assets/MRBike/Prefabs/BikeInteraction.prefab`](../Assets/MRBike/Prefabs/BikeInteraction.prefab) into your scene.

- In a **Photon Fusion** session the prefab is normally spawned at runtime by the `NetworkRunner`. Call `NetworkRunner.Spawn(bikeInteractionPrefab, ...)` from your session host after joining or starting a session.
- In a **single-player** setup (no networking), you can place it directly in the scene hierarchy and it will work once the Interaction SDK rig is active.

---

## Step 5 — Configure the BikeInteraction Prefab

Open the `BikeInteraction` prefab and verify the following Inspector references are set correctly for your project:

| Component | Field | What to assign |
|---|---|---|
| `BikeAppStarter` | `VONetworkPlayer` | The `VONetworkPlayer` component on the prefab |
| `BikeAppStarter` | `TaskHandler` | The `TaskHandler` component on the prefab |
| `NetworkTaskTracker` | `CourseTracker` | The `CourseTracker` component on the prefab |
| `VONetworkManager` | `TaskVOPlayer` | The `TaskVOPlayer` component on the prefab |

These references are already serialized in the prefab. Check them after importing to make sure Unity resolved all asset GUIDs correctly. If any reference is shown as **Missing**, re-assign it manually by dragging the corresponding child component.

---

## Step 6 — Verify Shaders and Materials

MRBike uses three custom URP shaders:

- `Assets/MRBike/Shaders/ContactShadow.shader`
- `Assets/MRBike/Shaders/Lighting.shader`
- `Assets/MRBike/Shaders/LightingAffordance.shader`

Open your destination project's **URP Asset** (under `Project Settings > Graphics`) and confirm the shaders compile without errors. If you see pink materials, check:

1. The URP package version matches (the source project uses `com.unity.render-pipelines.universal` 17.0.4).
2. The shaders and their associated material property names are intact after the copy.

---

## Step 7 — Verify Animations

The animation controllers and clips are fully self-contained inside `Assets/MRBike/Animations/`. No additional setup is required. After importing, open a clip (e.g., `axle.anim`) in the Animation window and confirm the curves target the correct GameObjects inside the `BikeInteraction` prefab hierarchy.

---

## Step 8 — Audio and Voice-Over

Audio clips are located in `Assets/MRBike/Audio/`:

- `SFX/` — Sound effects played by `BikeAudioTrigger` components.
- `VO/` — Voice-over clips played by `TaskVOPlayer` in task order.

The `TaskVOPlayer` component holds an ordered list of `AudioClip` references. After importing, verify that all clips in the `TaskVOPlayer` Inspector list show correctly and are not missing.

To enable Meta XR audio spatialization on the bike audio sources, ensure `com.meta.xr.sdk.audio` is installed and the **Meta XR Audio** spatializer plugin is selected under `Project Settings > Audio > Spatializer Plugin`.

---

## Step 9 — Build and Test

1. Set the build target to **Android** (`File > Build Settings > Android`).
2. Under **Player Settings**, set the architecture to **ARM64** and the minimum API level to **32** (Android 12L).
3. In `Meta > Tools > Project Setup Tool`, resolve any remaining issues (permissions, manifest entries, etc.).
4. Build and deploy to a Meta Quest device.
5. Test each assembly interaction:
   - Grab and place the wheel, axle, pedals, and seat.
   - Confirm voice-over cues play at each step.
   - Confirm haptic feedback fires when grabbing and releasing parts.
   - Confirm the course tracker markers light up as tasks are completed.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `[AutoSet]` / `[AutoSetFromParent]` compile errors | `com.meta.utilities` not installed | Add the package as described in Step 1 |
| `Fusion.NetworkBehaviour` compile errors | Photon Fusion not installed | Install Photon Fusion 1 as described in Step 1 |
| `Oculus.Interaction` compile errors | Meta XR Interaction SDK not installed | Install `com.meta.xr.sdk.interaction.ovr` |
| Pink / missing materials | Shader not compiled for URP version | Check URP version compatibility (Step 6) |
| Missing audio clips in `TaskVOPlayer` | GUIDs not resolved after copy | Re-assign clips manually in the Inspector |
| No haptic feedback | `OVRManager` not in scene or hand tracking not enabled | Add an `OVRManager`/`OVRCameraRig` to the scene (Step 4a) |
| Parts do not snap to target | `TransformTarget` `GrabbedObject` reference missing | Verify `NetworkedBikeObjectAssembly` references on each grabbable (Step 5) |

---

## Appendix A — Removing Photon Fusion Dependency

If you want a single-player, non-networked version of MRBike, replace the following `NetworkBehaviour`-based scripts with plain `MonoBehaviour` alternatives:

| Script | Networked features to replace |
|---|---|
| `BikeVisibleObject.cs` | `[Networked]` state, RPCs → use local state directly |
| `CourseTracker.cs` | `NetworkArray<NetworkBool>`, `Spawned()` → use a regular `bool[]` and `Start()` |
| `NetworkTaskTracker.cs` | `[Rpc]` → call `CourseTracker.CompleteTask()` directly |
| `VONetworkManager.cs` | `[Rpc]` → call `TaskVOPlayer.PlayOnce()` directly |
| `NetworkedBikeTools.cs` | Remove or stub out |

For each script, change `NetworkBehaviour` to `MonoBehaviour`, remove `using Fusion;`, remove `[Rpc]`, `[Networked]`, and `[Capacity]` attributes, and replace RPC calls with direct method calls.
