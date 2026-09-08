# AukiWrapper

## Overview

The `AukiWrapper` class is a Unity wrapper for the Auki ConjureKit SDK, providing a simplified interface for AR multiplayer functionality. It handles AR session management, entity synchronization, component broadcasting, and participant management in shared AR experiences.

## Purpose

- **AR Multiplayer Integration**: Bridges Unity's AR Foundation with Auki's ConjureKit for shared AR experiences
- **Entity Management**: Handles creation, deletion, and synchronization of AR entities across participants
- **Component Broadcasting**: Manages component-based data synchronization between participants
- **Session Management**: Controls joining/leaving multiplayer sessions and participant coordination

## Key Dependencies

- `Auki.ConjureKit` - Core Auki SDK
- `UnityEngine.XR.ARFoundation` - Unity AR Foundation
- `IAnalyticsService` - Analytics tracking
- `AukiSettings` - Configuration settings

## Architecture

### Core Components

- **ConjureKit**: Main Auki SDK instance for networking
- **Vikja**: Handles entity actions and interactions
- **Grund**: Manages AR calibration and spatial mapping
- **Manna**: Provides haptic feedback and calibration assistance

### AR Integration

- **ARCamera**: Unity AR camera for rendering
- **ARCameraManager**: Manages AR camera settings
- **ARRaycastManager**: Handles AR raycasting for interactions
- **AROcclusionManager**: Manages depth and occlusion settings
- **ARPlaneManager**: Handles plane detection (horizontal/vertical)

## Public Interface

### Properties

```csharp
Camera arCamera                    // AR camera instance
ARRaycastManager arRaycastManager // AR raycast manager
ARCameraManager arCameraManager    // AR camera manager
bool ready                        // Whether Auki is ready for use
bool isConnected                  // Whether connected to a session
bool isHost                       // Whether this participant is the host
float joinTimestamp               // When the session was joined
```

### Events

```csharp
Action onInit                                    // Called when Auki is initialized
Action onLeft                                    // Called when leaving session
Action<Session> onJoined                         // Called when joining session
Action<EntityAction> onEntityAction              // Called on entity interactions
Action<ComponentAddBroadcast> onComponentAdd     // Called when component is added
Action<ComponentUpdateBroadcast> onComponentUpdate // Called when component is updated
Action<ComponentDeleteBroadcast> onComponentDelete // Called when component is deleted
Action<CustomMessageBroadcast> onCustomMessageBroadcast // Called on custom messages
Action<uint> onParticipantLeft                   // Called when participant leaves
Action<Participant> onParticipantJoined          // Called when participant joins
Action<uint> onEntityDeleted                    // Called when entity is deleted
Action<Entity> onEntityAdded                     // Called when entity is added
Action<Entity> onEntityUpdatePose                // Called when entity pose updates
Action<State> onStateChanged                     // Called when Auki state changes
Action<uint, uint> onHostChanged                 // Called when host changes (old, new)
```

### Core Methods

#### Session Management

```csharp
void Install(Action onSuccess, Action onFail)           // Initialize Auki SDK
void Join(Action onComplete, Action<string> onFail)      // Join new session
void Join(string sessionId, Action onComplete, Action<string> onFail) // Join specific session
void Leave()                                              // Leave current session
```

#### Entity Management

```csharp
void AddEntity(Pose pose, bool persistent, Action<Entity> onEntityAdded, Action<string> onError)
void DeleteEntity(uint entityId, Action onComplete)
Entity GetEntity(uint entityId)
bool HasEntity(uint entityId)
bool IsMine(uint entity)  // Check if entity belongs to this participant
```

#### Component Management

```csharp
void AddComponent(Session session, uint componentTypeId, uint entityId, byte[] data, Action onComplete, Action<string> onError)
void UpdateComponent(Session session, uint componentTypeId, uint entityId, byte[] data)
void DeleteComponent(Session session, uint componentTypeId, uint entityId, Action onComplete, Action<string> onError)
void AddComponentType(Session session, string name, Action<uint> onComplete, Action<string> onError)
void SubscribeToComponentType(Session session, uint id, Action onComplete, Action<string> onError)
```

#### Communication

```csharp
bool SendCustomMessage(uint[] participantIds, byte[] data)
void BroadcastCustomMessage(byte[] data)
void SendCustomMessageToParticipant(uint participantId, byte[] data)
void RequestAction(uint entityId, string name, byte[] data, Action<EntityAction> onComplete, Action<string> onError)
```

#### Utilities

```csharp
void InstallManna(Action<Manna> onComplete)              // Install haptic feedback system
void MeasurePing(Action<double> onComplete, Action<string> onError)
NetworkQuality GetNetworkQuality()
Session GetSession()
State GetState()
```

## Usage Patterns

### Initialization

```csharp
// Install Auki SDK
aukiWrapper.Install(
    onSuccess: () => Debug.Log("Auki installed successfully"),
    onFail: () => Debug.LogError("Failed to install Auki")
);

// Join session
aukiWrapper.Join(
    onComplete: () => Debug.Log("Joined session"),
    onFail: (error) => Debug.LogError($"Failed to join: {error}")
);
```

### Entity Creation

```csharp
// Create persistent entity at specific pose
aukiWrapper.AddEntity(
    pose: new Pose(position, rotation),
    persistent: true,
    onEntityAdded: (entity) => Debug.Log($"Created entity {entity.Id}"),
    onError: (error) => Debug.LogError($"Failed to create entity: {error}")
);
```

### Component Broadcasting

```csharp
// Add component to entity
session.AddComponent(
    componentTypeId: typeId,
    entityId: entityId,
    data: serializedData,
    onComplete: () => Debug.Log("Component added"),
    onError: (error) => Debug.LogError($"Failed to add component: {error}")
);

// Update component
session.UpdateComponent(componentTypeId, entityId, updatedData);
```

## Configuration

### AukiSettings

The wrapper uses `AukiSettings` for configuration:

- `appKey` / `appSecret`: Auki API credentials. Not serialized in the config asset; filled at bootstrap from the gitignored `AppSecrets` asset generated from `.env` (see [Secrets.md](Secrets.md))
- `logLevel`: Logging verbosity
- `autoJoinOnStart`: Whether to auto-join on startup
- `useThisSessionIdInEditor`: Use specific session ID in editor
- `sessionId`: Default session ID
- `useGrund`: Enable Grund spatial mapping
- `cameraCullingMask`: AR camera culling mask
- `humanSegmentationDepthMode`: Human segmentation depth mode
- `humanSegmentationStencilMode`: Human segmentation stencil mode
- `environmentDepthMode`: Environment depth mode

## State Management

### Connection States

- **Disconnected**: Not connected to any session
- **Connecting**: Attempting to join session
- **Connected**: Successfully joined session
- **Ready**: Fully initialized and ready for use

### Host Determination

The host is determined by the lowest participant ID. The `isHost` property checks if the current participant has the lowest ID among all participants.

## Error Handling

- All async operations include error callbacks
- Connection state is managed to prevent race conditions
- Entity ownership tracking prevents unauthorized operations

## Performance Considerations

- Entity ownership is cached locally (`m_MyEntities`)
- Host determination could be optimized to only check on participant changes
- Component subscriptions are managed to avoid unnecessary updates

## Integration Notes

- Requires AR Foundation setup with proper camera and plane detection
- Analytics integration for session tracking
- Dependency injection compatible (implements `IAukiWrapper`)
- Thread-safe event handling for Unity main thread operations
