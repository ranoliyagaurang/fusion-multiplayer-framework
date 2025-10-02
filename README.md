# MultiplayerFramework

## Overview

**MultiplayerFramework** is a Unity package built on **Photon Fusion** that provides a ready-to-use multiplayer setup for teacher–student style interactions in VR/AR experiences. The framework makes it easy to create and join rooms, manage authority, and control classroom-like sessions with advanced admin tools for teachers.

---

## ✨ Features

* **Room Management**

  * Teacher can **create** and **join rooms**.
  * Students can **join rooms** created by teachers.

* **Teacher Admin Controls**

  * Disable student **movement**, **microphone**, and **grabbing permissions**.
  * Hide a specific student’s avatar locally.
  * View and manage the full **student list**.

* **XR Features**

  * Teacher can enable **passthrough** mode for all students simultaneously.
  * Both Teacher and Students can **select avatars** before creating or joining rooms.

---

## 📦 Required Assets (Dependencies)

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