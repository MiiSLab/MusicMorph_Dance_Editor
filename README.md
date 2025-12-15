# MusicMorph Dance Editor

<div align="center">

![Demo](docs/demo.gif)

**A VR-Based Interactive Dance Motion Editing System with AI-Powered Generation**

[Demo Video](docs/demo.mp4)

</div>

## 📖 Overview

MusicMorph is a Unity-based VR interactive dance motion editing system that incorporates AI-driven dance generation technology. Published in IEEE AIxVR 2026, this system provides an immersive environment that enables users to create, edit, and optimize dance motions through multiple approaches.

### Core Features

-   **🎵 Music-Driven Dance Generation**: Automatically generates dance motions based on music features
-   **🎯 Multi-Modal Editing Approaches**:
    -   **Text-Based Motion Editor**: Edit specific dance segments through textual descriptions
    -   **Manual Motion Editor**: Direct joint manipulation using VR controllers for fine-grained editing
    -   **Interactive Timeline**: Visualized timeline interface supporting music beat and energy analysis
-   **👥 Multi-Character System**: Support for selecting and configuring multiple virtual characters performing simultaneously
-   **🎮 Immersive VR Experience**: Full support for Meta Quest VR devices
-   **💾 Data Management**: Local and server data synchronization, supporting dance data storage and retrieval

## 🎯 Key Features

### 1. AI-Powered Dance Generation

The system analyzes music features (rhythm, energy, melody) through a backend AI model to automatically generate dance motions synchronized with the music.

### 2. Dual-Mode Editing System

**Text-Based Editing**

```
1. Select time range (dual slider)
2. Input motion description (e.g., "wave hands", "spin around")
3. System generates corresponding motions and seamlessly integrates them
```

**Manual VR Editing**

```
1. Pause at a specific frame
2. Select joints using controllers
3. Directly drag to adjust positions
4. Save modifications
```

### 3. Real-Time Music Visualization

-   **Beat Markers**: Visualize music beat points to help align motions
-   **RMS Energy Curve**: Display music energy variations to assist with motion intensity design
-   **Timeline Navigation**: Precise frame-level control and range selection

### 4. Multi-Character Performance

Supports multiple virtual characters simultaneously performing the same dance, showcasing group dance effects.

## 🚀 Getting Started

### Prerequisites

-   **Unity Version**: Unity 2021.3 LTS or higher
-   **VR Device**: Meta Quest 2/Pro or OpenXR-compatible devices
-   **Operating System**: Windows 10/11
-   **Backend Server**: Python Flask server (dance generation service)

### Installation

1. **Clone Repository**

    ```bash
    git clone https://github.com/your-repo/MusicMorph_Dance_Editor.git
    cd MusicMorph_Dance_Editor
    ```

2. **Open Unity Project**

    - Launch Unity Hub
    - Click "Open Project"
    - Select this project folder

3. **Configure VR Settings**

    - Navigation: `Edit → Project Settings → XR Plug-in Management`
    - Enable `Oculus` or `OpenXR`

4. **Set Backend Server URL**
    - Edit the `baseUrl` parameter in `HttpService.cs`
    - Or configure through Unity Editor interface

## 🎮 Usage

### Basic Workflow

1. **Launch System**

    - Open `Final.unity` scene
    - Deploy to VR device through Unity or use VR simulator

2. **Select Characters**

    - Drag and drop characters to slots in the character selection interface
    - Click "Apply" to confirm selection

3. **Load Dance Data**

    - Select dance from folder list
    - System automatically loads CSV (motion), JSON (metadata), Audio (music)

4. **Editing Mode**

    **Option A: Text-Based Editing**

    - Enter Text-Based Motion Editor
    - Use dual slider to select editing range
    - Input motion description and generate

    **Option B: Manual Editing**

    - Enter Manual Motion Editor
    - Select joints using VR controllers
    - Directly adjust poses

5. **Preview and Save**
    - Play to preview editing results
    - Save modifications to local or server

### VR Controller Operations

| Operation    | Controller Input                     |
| ------------ | ------------------------------------ |
| Select Joint | Trigger button (aim at joint sphere) |
| Move Joint   | Hold Trigger + move controller       |
| Confirm Edit | Grip button                          |
| Deselect     | A/X button                           |

## 📊 Data Format

### CSV Motion Data

Each row represents a frame, formatted as:

```
joint1_x, joint1_y, joint1_z, joint2_x, joint2_y, joint2_z, ...
```

Supported joint indices (based on SMPL skeleton):

-   0: Pelvis
-   1: L_Hip
-   2: R_Hip
-   3: Spine1
-   ... (24 joints in total)

### JSON Metadata

```json
{
  "id": "dance_001",
  "title": "Sample Dance",
  "duration": 30.5,
  "fps": 30,
  "beats": [0.5, 1.0, 1.5, ...],
  "rms": [0.2, 0.4, 0.6, ...]
}
```
