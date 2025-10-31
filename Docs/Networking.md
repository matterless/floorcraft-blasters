# Networking (ConjureKit / Posemesh)

This document explains how multiplayer networking is implemented using Auki Labs' ConjureKit SDK and related modules, and how it is integrated into the project.

## Overview

- Core multiplayer is provided by ConjureKit (Posemesh) with optional modules:
  - Manna: instant calibration into posemesh domains via QR, optional auto-join
  - Vikja: shared key–value store per Entity (lightweight sync)
  - Grund: shared plane/height alignment across participants
- The app wraps ConjureKit via `IAukiWrapper`/`AukiWrapper`, and orchestrates joining and readiness via `ConnectionService`.
- After joining, ECS is initialized and entity/component broadcasts flow to services.

References:

- ConjureKit introduction and modules: https://conjurekit.dev/unity/

## Packages used

- `com.aukilabs.unity.conjurekit` (Core)
- `com.aukilabs.unity.manna` (Manna)
- `com.aukilabs.unity.vikja` (Vikja)
- `com.aukilabs.unity.grund` (Grund)

See ConjureKit docs for details on each: https://conjurekit.dev/unity/

## Wrapper abstraction

- `IAukiWrapper` exposes:
  - ARFoundation handles: `Camera`, `ARCameraManager`, `ARRaycastManager`
  - Connection state: `ready`, `isConnected`, `isHost`, timestamps
  - Events: session join/leave, participant join/leave, entity add/delete/update pose, component add/update/delete, custom messages, host changed, state changes
  - Methods: `Install`, `InstallManna`, `Join`/`Leave`, `AddEntity`/`DeleteEntity`, `BroadcastCustomMessage`, target custom messages
- `AukiWrapper`:
  - Constructs `ConjureKit` with app keys and AR camera transform
  - Instantiates `Vikja` and `Grund`, subscribes to all ConjureKit events
  - Optionally auto-connects (or connects to a specific session in-editor)
  - Provides helper APIs for entities and messaging

## Session lifecycle (ConnectionService)

- Startup:
  - `ConnectionService` subscribes to `IAukiWrapper` events and kicks off `Join()`
  - On `onJoined(Session)`: caches session ID, raises `SessionInitialising`, starts ECS initialization
  - On ECS init success: sets `Connected` and marks networking ready
- Reconnection:
  - Reacts to app resume and network status changes
  - Leaves and reconnects with a short delay when needed
  - Times out ECS init on slow devices and retries
- Auto-connect:
  - If an auto-connect config exists, will join the host's session ID or create a new host session

## Calibration and domains (Manna)

- Manna enables instant calibration to a shared coordinate space using a QR code of known size
- The QR can encode the domain/session; the wrapper calls `InstallManna()` during setup
- Once calibrated, Unity world space aligns across devices for centimeter-level consistency

Docs: https://conjurekit.dev/unity/ (Manna section)

## Entity and property sync (Vikja)

- Vikja provides a shared key–value store attached to Entities
- State changes broadcast to all participants; new joiners receive current state
- Used for lightweight synchronization scenarios without full ECS complexity

Docs: https://conjurekit.dev/unity/ (Vikja section)

## Shared plane alignment (Grund)

- Grund keeps horizontal planes aligned visually across devices (no floating/sinking)
- Constructed with `ConjureKit` and `Vikja` to share plane heights

Docs: https://conjurekit.dev/unity/ (Grund section)

## ARFoundation integration

- ConjureKit integrates with ARFoundation; the wrapper caches:
  - `Camera` and `ARCameraManager` from `ARSessionOrigin`
  - `ARRaycastManager` for plane/hit testing

Docs: https://conjurekit.dev/unity/ (ARFoundation Integration)

## Configuration and binding

- Keys & settings are provided via `AukiSettings` (in `AppConfigs`) and bound in installers
- `RootInstaller` binds `IAukiWrapper` to `AukiWrapper` (or `AukiWrapperMock` in Editor), and `IMannaService`
- `ConnectionService` is constructed with `IAukiWrapper`, `IECSController`, `INetworkService`, and app runners/dispatchers

## Message channels

- Custom messages:
  - Broadcast: wrapper forwards to all participants
  - Targeted: wrapper can send to a specific participant
- Entity/component events:
  - `OnEntityAdded`, `OnEntityDeleted`, `OnEntityUpdatePose`
  - `OnComponentAdd`, `OnComponentUpdate`, `OnComponentDelete`
- App services subscribe via `IAukiWrapper` events and feed ECS/services

## Troubleshooting

- If ECS initialization hangs: ConnectionService triggers a timeout and reconnects
- On network drop: ConnectionService leaves and reconnects after a short delay
- Editor testing: mock wrapper can be bound from `RootInstaller` for local runs

## See also

- Auki Wrapper code and events (project): `Assets/_matterless/Scripts/Runtime/Multiplayer/AukiWrapper.cs`
- Connection flow controller: `Assets/_matterless/Scripts/Runtime/Connection/ConnectionService.cs`
- ConjureKit docs: https://conjurekit.dev/unity/

