# ECSController

## Overview

The `ECSController` class manages an Entity Component System (ECS) architecture for synchronized multiplayer entities in AR experiences. It handles component registration, entity management, and real-time synchronization of component data across participants using the Auki networking layer.

## Purpose

- **Component System Management**: Provides a structured way to manage entity components in multiplayer AR
- **Real-time Synchronization**: Handles component data synchronization across all participants
- **Entity Lifecycle**: Manages entity creation, updates, and deletion
- **Type Safety**: Provides type-safe component access and management
- **Factory Pattern**: Uses factory functions for component creation and deserialization

## Key Dependencies

- `IAukiWrapper` - Auki networking wrapper for multiplayer communication
- `IEntityComponentModel` - Interface for component data models
- `BidirectionalDictionary` - Custom dictionary for type ID ↔ name mapping

## Architecture

### Core Data Structures

```csharp
// Component factory functions: componentTypeName → factory function
Dictionary<string, Func<uint, uint, bool, IEntityComponentModel>> m_ComponentModelFactoryFunctions

// Entity components: entityId → {componentTypeId → component model}
Dictionary<uint, Dictionary<uint, IEntityComponentModel>> m_EntityDictionary

// Type mapping: componentTypeId ↔ componentTypeName
BidirectionalDictionary<uint, string> m_ComponentTypesMapping

// Registered component types for filtering
List<uint> m_RegisterComponents
```

### State Management

```csharp
public enum ECSState
{
    None,           // Not initialized
    Initialising,   // Currently initializing
    Succeed         // Successfully initialized
}
```

## Public Interface

### Events

```csharp
Action<ECSState> onStateChanged                    // Called when ECS state changes
Action<IEntityComponentModel> onOtherAdded         // Called when other participant adds component
Action<IEntityComponentModel> onOtherUpdate       // Called when other participant updates component
Action<string, uint, bool> onDeleted              // Called when component deleted (typeName, entityId, isMine)
```

### Core Methods

#### Initialization

```csharp
void Initialise(Session session, Action onSuccess, Action onError)
void Clear()
```

#### Component Registration

```csharp
void RegisterOnCreateEntityComponentModelFunction(
    string typeName,
    Func<uint, uint, bool, IEntityComponentModel> createEntityComponentEvent)
```

#### Component Management

```csharp
void AddComponent(IEntityComponentModel model)                    // Add component to entity
void BroadcastComponent(IEntityComponentModel model)              // Update component across network
void DeleteComponentFromEntity(string componentType, uint entityId) // Delete component from entity
```

#### Component Access

```csharp
IEntityComponentModel GetEntityComponentModel(string typeName, uint entityId)
bool TryGetEntityComponentModel<M>(string typeName, uint entityId, out M entityComponentModel)
uint GetComponentIdByName(string name)
```

## Usage Patterns

### Initialization Flow

```csharp
// 1. Register component factory functions
ecsController.RegisterOnCreateEntityComponentModelFunction("Position", CreatePositionComponent);
ecsController.RegisterOnCreateEntityComponentModelFunction("Health", CreateHealthComponent);

// 2. Initialize with session
ecsController.Initialise(
    session: aukiSession,
    onSuccess: () => Debug.Log("ECS initialized successfully"),
    onError: () => Debug.LogError("ECS initialization failed")
);
```

### Component Registration

```csharp
// Register a component type with factory function
ecsController.RegisterOnCreateEntityComponentModelFunction(
    "PlayerData",
    (typeId, entityId, isMine) => new PlayerDataComponent(typeId, entityId, isMine)
);
```

### Adding Components

```csharp
// Create and add component
var playerData = new PlayerDataComponent(componentTypeId, entityId, true);
playerData.playerName = "Player1";
playerData.score = 100;

ecsController.AddComponent(playerData);
```

### Updating Components

```csharp
// Get existing component and update
if (ecsController.TryGetEntityComponentModel<PlayerDataComponent>("PlayerData", entityId, out var playerData))
{
    playerData.score += 10;
    ecsController.BroadcastComponent(playerData);
}
```

### Component Access

```csharp
// Safe component access
if (ecsController.TryGetEntityComponentModel<PositionComponent>("Position", entityId, out var position))
{
    Vector3 pos = position.position;
    // Use position data
}
```

## Component Lifecycle

### 1. Registration Phase

- Component types are registered with factory functions
- Each component type gets a unique ID from Auki
- Component subscriptions are established

### 2. Initialization Phase

- Existing entities and components are fetched from the session
- Component models are created using factory functions
- Local entity dictionary is populated

### 3. Runtime Phase

- Components can be added, updated, or deleted
- Changes are synchronized across all participants
- Event notifications are sent for component changes

### 4. Cleanup Phase

- Entity dictionary is cleared when leaving session
- Component registrations are maintained for reconnection

## Component Model Interface

Components must implement `IEntityComponentModel`:

```csharp
public interface IEntityComponentModel
{
    uint typeId { get; }      // Component type ID
    uint entityId { get; }    // Entity ID
    byte[] data { get; }      // Serialized data

    void Serialize();         // Convert model to byte array
    void Deserialize(byte[] data); // Convert byte array to model
}
```

## Synchronization Flow

### Adding Components

1. **Local Registration**: Component is added to local entity dictionary
2. **Serialization**: Component data is serialized to byte array
3. **Network Broadcast**: Component is sent to Auki session
4. **Remote Processing**: Other participants receive and process the component

### Updating Components

1. **Local Update**: Component model is updated locally
2. **Serialization**: Updated data is serialized
3. **Network Broadcast**: Update is sent to all participants
4. **Remote Update**: Other participants update their local models

### Deleting Components

1. **Network Deletion**: Deletion request is sent to Auki
2. **Local Cleanup**: Component is removed from local dictionary
3. **Event Notification**: Deletion event is fired

## Error Handling

### Initialization Errors

- Component registration failures are handled gracefully
- Session disconnection during initialization cancels the process
- Error callbacks are provided for all async operations

### Runtime Errors

- Missing entities/components are handled with warnings
- Type safety is enforced through generic methods
- Exception throwing methods are marked for removal

## Performance Considerations

### Memory Management

- Entity dictionary uses lazy initialization
- Component models are cached locally to avoid repeated deserialization
- Bidirectional dictionary provides O(1) type ID ↔ name lookups

### Network Optimization

- Only registered component types are processed (filtering)
- Component data is serialized only when needed
- Batch operations could be implemented for multiple updates

### Thread Safety

- All operations are designed for Unity main thread
- Event handling is thread-safe
- Dictionary operations are not thread-safe (Unity main thread only)

## Integration Notes

### Auki Integration

- Depends on `IAukiWrapper` for all networking operations
- Listens to Auki events for component synchronization
- Uses Auki's component type system for type management

### Dependency Injection

- Constructor injection compatible
- Implements `IECSController` interface
- Can be easily mocked for testing

## Debugging and Logging

### Initialization Logging

- Component registration progress is logged
- Session entity counts are reported
- Initialization completion is clearly marked

### Runtime Logging

- Component additions/updates are logged with data details
- Entity operations include entity and component IDs
- Warning messages for missing entities/components

## Future Improvements

### Marked for Removal

```csharp
// These methods throw exceptions and should be removed
void AddComponentToEntity(string componentType, uint entityId, byte[] data)
void UpdateComponentOnEntity(string componentType, uint entityId, byte[] data)
```

### Potential Enhancements

- Batch component operations
- Component versioning for conflict resolution
- Automatic component cleanup on entity deletion
- Performance profiling and optimization
