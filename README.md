# MultiplayerFramework

## Overview

**MultiplayerFramework** is a Unity package built on **Photon Fusion** that provides a ready-to-use multiplayer setup for teacher–student style interactions in VR/AR experiences. The framework makes it easy to create and join rooms, manage authority, and control classroom-like sessions with advanced admin tools for teachers.

---

Perfect 👍 I’ll keep it **clean and simple** but add in all the new features you mentioned. Here’s the updated **categorized feature list** for your VR multiplayer welding classroom:

---

# ✨ Features Overview

 👩‍🏫 **Room & User Management**

* Teacher can **create** and **join rooms** (room creation restricted with a **password authentication** so only teachers can create rooms).
* Students can **join rooms** created by teachers.
* Supports **multiple teachers** in the same room → all act as **admins** with full control.
* **Avatar selection**: Choose from **10 different 3D avatars** before joining a room.
* **Name selection**: Players set a display name that appears above their avatar in-game.
* Teacher can **view and manage** the complete student list.
* Teacher can **reposition students** anywhere in the VR space for demonstrations.

 🛠️ **Teacher Admin Controls**

* Disable student **movement**, **microphone**, and **grabbing permissions**.
* Hide a specific student’s avatar locally.
* **Mute/unmute students** or allow students to mute themselves.
* **Network monitoring**: Teacher(s) can see **ping/latency** for all players including themselves.
* Admin privileges extend to **all teachers** in the room (shared control).

 🎤 **Voice & Communication**

* **Realtime synchronized voice chat**, works like a natural group call.
* Students can **mute themselves** independently.

 🖥️ **XR Teaching Tools**

* Teacher can enable **passthrough mode** for all students simultaneously.
* **Raycast pointers**:

  * Students see their own hand raycasts (for interaction only, not visible to others).
  * Teacher’s **raycasts are synced** and visible to all students (laser-pointer style).
* **PPT and video panels**:

  * Synchronized across all players.
  * Only teacher(s) can **play/pause videos** and **change slides**.
* **Smartboard system**:

  * Teacher can grab a marker in different colors.
  * Draw on the board in realtime, visible to all students.

 🔧 **Learning Environment**

* Teachers can move students into position for **hands-on demonstrations**.
* Collaborative tools (slides, videos, whiteboard, laser pointers) make lessons feel **like a real classroom**.
* Supports **multiple teacher-led sessions** in the same room.

---

📦 Required Assets (Dependencies)

Before installing this package, please make sure to import the following Unity Asset Store packages into your project:

1. [**Meta XR All-in-One SDK**](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657)

   * Provides XR core integration for Meta Quest devices.
   * Includes features like Passthrough, Hand Tracking, and Mixed Reality tools.

2. [**Photon Fusion**](https://assetstore.unity.com/packages/p/photon-fusion-267958)

   * High-performance multiplayer networking engine.
   * Handles room creation, synchronization, and network authority for teacher–student interactions.

3. [**Photon Voice 2**](https://assetstore.unity.com/packages/p/photon-voice-2-130518)

   * Real-time voice communication system.
   * Required for enabling teacher microphone control over students.

⚠️ These packages are **mandatory**. Please install them **before adding this framework package** to avoid missing dependency errors.

---

## 🔧 Installation

1. Import the required assets listed above into your Unity project.

2. Add this package as a Git dependency in your project’s `manifest.json`:

   ```json
   "dependencies": {
     "com.yourname.fusion-multiplayer-framework": "https://github.com/yourname/FusionMultiplayerFramework.git#1.0.0"
   }
   ```

   Or add it directly via **Unity Package Manager** → **Add package from Git URL**.

3. Once imported, open the included **Samples** (under Package Manager → Samples) to explore example Teacher–Student setups.

---

## 📖 Usage

* Teachers can create and join rooms through provided prefabs and scripts.
* Students connect to existing rooms via the same framework.
* Admin controls are accessible through a Teacher UI (disable student actions, hide avatars, enable passthrough).
* Avatar selection is available before room entry for both roles.

---

## 🧩 Samples

The package includes sample prefabs and demo scenes located under `Samples~`:

* **Teacher & Student Demo**: Example scene showcasing basic teacher–student interaction.
* **Passthrough Demo**: Example setup showing how passthrough can be enabled across all connected students.

---

## 📄 License & Support

* Please ensure you comply with the license terms of **Meta XR SDK**, **Photon Fusion**, and **Photon Voice 2**. These assets are not bundled in this package and must be acquired separately from the Unity Asset Store.
* This framework is provided as a reusable base for building multiplayer classroom experiences.

For support, issues, or feature requests, please contact **[Gaurang Ranoliya]** or open an issue on the GitHub repository.