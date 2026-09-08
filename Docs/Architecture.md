# Architecture Overview

This document describes the high-level architecture of Floorcraft: Blasters with a focus on dependency injection, service composition, UI/game flow, and extensibility.

## Core Concepts

- **Engine**: Unity (URP, AR Foundation)
- **DI Framework**: `Matterless.Inject` using `MonoInstaller`
- **Composition Roots**: Scene-level installers (`RootInstaller`, `AppInstaller`, `UiInstaller`)
- **Patterns**: Constructor injection, config via `ScriptableObject` instances, interface-to-implementation bindings
- **Major Subsystems**: AR/XR, Auki Posemesh networking, ECS gameplay services, UI flow/state machine, Wallet/NFT, Analytics

## Dependency Injection Model

The project uses `Matterless.Inject` to construct and wire services at runtime via installers. Services declare dependencies in their constructors, keeping them testable and decoupled from Unity scene concerns.

- `container.Bind<TService, TImpl>([args])`: Binds an interface to a concrete type.
- `container.Bind<TService>([args])`: Binds a concrete service.
- `container.BindInstance(obj)`: Injects existing instances (Unity objects or ScriptableObject settings).

Settings and Unity objects are typically `BindInstance`-ed first, followed by services that consume them via constructor injection.

## Installers and Responsibilities

### RootInstaller (Platform & Cross-Cutting)

Binds platform/runtime facilities and cross-cutting services. Typical bindings:
- API keys: loads the gitignored `AppSecrets` asset (generated from `.env`, see [Secrets.md](Secrets.md)) and applies it to `AppConfigs` before anything else is bound
- Rendering setup, `ARSession` and `ARSessionOrigin`
- Auki Posemesh integration (`IAukiWrapper`, `MannaService`)
- Analytics, Audio, Remote Config, Localisation, REST, Input Dialogue
- Unity helpers: `ICoroutineRunner`, `IUnityEventDispatcher`
- Splash screen service and the `Bootstrap` composition object

Key traits:
- Uses editor-aware bindings (mock vs real for Auki)
- Feeds `ScriptableObject` settings via `BindInstance`

### AppInstaller (Gameplay & Domain)

Binds game domain services and ECS components. Typical bindings:
- Domain/config settings from `AppConfigs` (raycast, vehicles, obstacles, scoring, equipment, respawn, menu, leaderboard, mayhem, wallet, chain, etc.)
- Gameplay services: `SpeederService`, weapons (laser/flamethrower/wrecking ball magnet), obstacles, power-ups, cooldown, respawn, leaderboard, haptics, notifications
- ECS: `IECSController`, `IComponentModelFactory`, and per-component services
- Multiplayer domain: `IDomainService`, `IHeartbeatService`, placement
- Wallet/NFT: `WalletService` (consumes `WalletSettings` and `ChainSettings`)

Key traits:
- Settings first, services second; ECS is modular to isolate component logic
- Central place to add new gameplay features

### UiInstaller (UI & Flow)

Binds UI controllers and flow/state machine orchestration. Typical bindings:
- UI settings (vehicle selector, renderer, audio UI)
- UI services: `HeaderUiService`, `SidebarUiService`, `IntroUiService`, `SpawningService`, `QrCodeUiService`, `ObstaclesUiService`
- Renderer abstraction: `IRendererService`
- Flow: `UiFlowService`
- Wallet UI: `WalletUiService`

Key traits:
- Consumes services from Root/App installers; responsible for user-facing orchestration

## UI/Game Flow and State Machine

`UiFlowService` coordinates app states using a simple state machine (`Matterless.StateMachine`):

States:
- Intro → VehicleSelector → Spawning → Gameplay → Spectator

Behavior:
- On state transitions, shows/hides UI panels and triggers service calls (e.g., spawn, despawn, respawn)
- Subscribes to connection and orientation events
- Integrates wallet UI and mayhem/game over flows

Benefits:
- Centralized, explicit flow control that stays testable and scalable as UI grows

## Wallet and NFT Integration

- Wallet: Reown AppKit (WalletConnect) is initialized via `WalletService`, which:
  - Manages connection/disconnection and exposes wallet address
  - Fetches balances (native and AUKI token) via direct JSON-RPC
  - Builds an NFT ownership cache for configured token IDs
- NFT: Implemented as ERC-1155 read-only access using Nethereum with JSON-RPC fallbacks
  - `NFTService` provides `OwnsToken`, `GetTokenURI`, and sprite loading from metadata with IPFS gateway support
  - `VehicleSelectorService` gates vehicles using `Vehicle.requiresNFT` and `Vehicle.nftTokenId`, consulting `WalletService` cache
  - `WalletUiService` displays owned NFTs as images in dynamically created containers

## ECS Gameplay Services

ECS is composed via DI:
- `IECSController` and `IComponentModelFactory` orchestrate component updates
- Component services (e.g., Transform, Properties, Score, SpeederState) are bound concretely for clarity

This keeps networked and simulated entities modular and makes adding new components straightforward: bind the service and register in the factory/controller.

## Settings and Configuration

Settings live in `ScriptableObject` assets and are aggregated in `AppConfigs`:
- Root: auki/manna, network, rendering, audio UI, splash/version
- App: gameplay settings (vehicles, obstacles, power-ups, equipment, respawn, menu, leaderboard), wallet and chain settings
- UI: renderer/audio UI and UI-specific configs

Installers `BindInstance` these settings so services remain pure and environment-agnostic.

## Extending the Architecture

- New platform/cross-cutting service → add to `RootInstaller`
- New gameplay/domain service or ECS component → add to `AppInstaller`
- New UI controller/view or flow step → add to `UiInstaller`
- New settings → extend `AppConfigs`, then `BindInstance` in the relevant installer

Guidelines:
- Prefer interface bindings to allow mocks and future replacements
- Keep constructors explicit; avoid service locators or static singletons
- Inject Unity dependencies (e.g., `MonoBehaviour` context) via installer arguments where necessary

## Directory Pointers

- `Assets/_matterless/Scripts/Runtime/MonoInstallers/`: DI composition roots
- `Assets/_matterless/Scripts/Runtime/MainHUD/`: UI flow and services
- `Assets/_matterless/Scripts/Runtime/ECS/`: ECS controller and component services
- `Assets/_matterless/Scripts/Runtime/Wallet/` and `.../NFT/`: WalletConnect and ERC-1155 integration
- `Assets/_matterless/Data/`: Project `ScriptableObject` configs (`AppConfigs`, settings)

## Summary

The architecture emphasizes clear composition via installers, constructor-injected services, and `ScriptableObject` configurations. Root/App/UI layers separate concerns between platform/cross-cutting, gameplay domain, and user interface/flow. Wallet/NFT and ECS subsystems are integrated through the same DI patterns, making the system cohesive and extensible.
