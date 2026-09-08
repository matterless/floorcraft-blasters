# Floorcraft: Blasters

A multiplayer augmented reality racing game built with Unity, featuring real-time spatial multiplayer powered by Auki's Posemesh SDK, Web3 wallet integration, and NFT support.

## Features

- **AR Foundation** - Cross-platform AR for iOS and Android
- **Spatial Multiplayer** - Share AR experiences using Auki's Posemesh (ConjureKit, Manna, Grund)
- **Domain Sessions** - Join persistent shared AR spaces via QR codes
- **Web3 Integration** - Wallet connectivity via Reown (WalletConnect)
- **NFT Support** - ERC-1155 token ownership verification on Base blockchain
- **Multiple Game Modes**:
  - Free For All - Solo racing
  - Join Session - Multiplayer via QR code
  - Mayhem Mode - PvE with AI enemies

## Paid Assets (optional)

The official builds use two paid assets that are not included here: AVPro Movie
Capture (video recording) and the EffectCore explosion pack (particle materials).
The project compiles and runs without them; recording is disabled and the
affected particles show missing materials. Licensed developers can install them
with `tools/paid-assets.sh install`. See [Docs/PaidAssets.md](Docs/PaidAssets.md).

## Getting Started

### Prerequisites

- **Unity 2022.3.62f2** (or compatible 2022.3.x LTS version)
- **Platform SDKs:**
  - iOS: Xcode 14+ and iOS 14+ device
  - Android: Android SDK with API Level 26+ (Android 8.0+)
- **Git** for cloning the repository
- **ARCore/ARKit compatible device** (AR features won't work in Unity Editor camera view)

### Installation

1. **Clone the repository:**

   ```bash
   git clone <repository-url>
   cd floorcraft-blasters
   ```

2. **Open in Unity:**

   - Launch Unity Hub
   - Click "Open" or "Add"
   - Select the `floorcraft-blasters` folder
   - Unity will import all assets (this may take several minutes on first open)

3. **Verify dependencies:**
   - Unity Package Manager should automatically resolve packages
   - Check that these packages are installed:
     - AR Foundation (4.2.9)
     - Auki ConjureKit
     - Matterless modules (Audio, Localisation, Inject, Native Share)

### Initial Configuration

Before running the project, you **must** configure the following:

1. **API keys** go into `.env`, never into the config assets (see [Docs/Secrets.md](Docs/Secrets.md)):

   ```bash
   cp .env.example .env
   ```

   - `AUKI_APP_KEY` and `AUKI_APP_SECRET` from [Auki Console](https://console.auki.network), required for multiplayer
   - `AMPLITUDE_API_KEY` from [Amplitude](https://amplitude.com), required for analytics
   - `REOWN_PROJECT_ID` and `ALCHEMY_API_KEY` for the optional wallet and NFT features (see the sections below)

   The editor generates the gitignored `AppSecrets` asset from this file whenever scripts
   reload and before every build. Without a `.env` the project still opens and runs, with
   multiplayer, analytics, wallet, and NFT features disabled. The same file carries the
   optional deployment identity (posemesh domain id, Android package name and keystore),
   so the tracked config assets and Player Settings only hold placeholders.

2. **Push guard** (recommended for contributors): install the pre-push hook that refuses
   commits containing `.env`, API keys, or the paid assets:

   ```bash
   tools/paid-assets.sh hook
   ```

3. **Auki domain** (optional): set `AUKI_DOMAIN_ID` in `.env`; the config assets no longer hold it.

4. **Optional Integrations:**
   - Reown WalletConnect (see Wallet Integration section below)
   - Alchemy/Base NFT (see NFT Integration section below)
   - Backtrace crash reporting
   - Joystick remote config

### Building and Running

Before every build the editor regenerates the `AppSecrets` asset from `.env`, so the keys
present in `.env` (or in environment variables with the same names, for CI) at build time
are the ones that ship.

#### Testing in Unity Editor

**Note:** AR camera won't work in Editor, but you can test multiplayer logic:

1. **Quick Session Testing (Editor Only):**

   - Press **D-O-M** keys in sequence to simulate scanning a domain QR code
   - Press **D-E-V** keys in sequence to open debug menu

2. **Start Play Mode:**
   - Click the Play button
   - You'll see UI but no AR camera feed (expected in Editor)

#### Building for iOS

1. **Configure iOS Build Settings:**

   - File → Build Settings
   - Select iOS platform
   - Switch Platform (if needed)

2. **Player Settings:**

   - Set Bundle Identifier (e.g., `com.yourcompany.floorcraft`)
   - Set Camera Usage Description
   - Minimum iOS Version: 14.0

3. **Build:**
   - Click "Build" and select output folder
   - Open generated Xcode project
   - Connect iOS device and run from Xcode

#### Building for Android

1. **Configure Android Build Settings:**

   - File → Build Settings
   - Select Android platform
   - Switch Platform (if needed)

2. **Player Settings:**

   - Set Package Name
   - Minimum API Level: 26 (Android 8.0), required by AVPro Movie Capture

3. **Build and Run:**
   - Connect Android device via USB
   - Enable USB Debugging on device
   - Click "Build and Run"

For a Google Play upload, select `Assets/_matterless/_BuildConfigs/BC_Floorcraft_Blasters_Android`
and press **Build Android App Bundle** in its Inspector. The config sets version, version code
and defines and writes the `.aab` to the `Builds` folder. The store package name and the
release keystore come from `ANDROID_PACKAGE_NAME`, `ANDROID_KEYSTORE_PATH`,
`ANDROID_KEYSTORE_ALIAS` and the two password variables in `.env`; they are applied for the
build only, so the tracked Player Settings keep their neutral values.

### Project Structure

```
Assets/
├── _matterless/
│   ├── Data/                    # ScriptableObject configs (no API keys; those come from .env)
│   ├── Resources/AppSecrets.asset  # Generated from .env, gitignored
│   ├── Scenes/                  # Main game scene
│   ├── Scripts/
│   │   ├── Runtime/
│   │   │   ├── Multiplayer/     # Auki/ConjureKit integration
│   │   │   ├── MainHUD/         # UI services
│   │   │   ├── ECS/             # Entity Component System for networked objects
│   │   │   ├── NFT/             # Web3 wallet & NFT integration
│   │   │   └── Analytics/       # Amplitude analytics
│   │   └── Editor/              # Unity Editor tools
│   └── Modules/                 # Matterless framework modules
└── Packages/
    └── manifest.json            # Package dependencies
```

### First Run Checklist

- [ ] Unity version 2022.3.62f2 installed
- [ ] Project opens without errors
- [ ] `.env` created from `.env.example` with the Auki and Amplitude keys
- [ ] Console shows `[Secrets] Assets/_matterless/Resources/AppSecrets.asset updated from .env`
- [ ] AR-capable device available for testing
- [ ] Build successfully deploys to device
- [ ] AR camera activates on device launch

### Troubleshooting

**"[AppSecrets] Resources/AppSecrets.asset is missing" error:**

- Create `.env` from `.env.example` and fill in the keys
- Run **Matterless > Secrets > Sync from .env** (or let scripts reload)
- The project still runs without it, but keyed features stay disabled

**AR camera shows black screen:**

- Ensure you're running on a real device (not simulator/editor)
- Check camera permissions in device settings

**Multiplayer connection fails:**

- Verify `AUKI_APP_KEY` and `AUKI_APP_SECRET` in `.env` are correct, then re-sync
- Check device internet connection
- Ensure you're on the correct build environment (staging/prod)

**Build errors on iOS/Android:**

- Verify minimum OS versions meet requirements
- Check that AR Foundation and ARCore/ARKit XR Plugins are installed

---

## Configuration Details

The `_matterless/Data` folder contains configurations, some of which you'll need to update with your own information.

- `Blasters Configs` and `Floorcraft Configs` hold configurations for privacy policy, terms of services, Auki posemesh domain id, wallet metadata, and NFT contracts. API keys are not in these assets; they come from `.env` (see [Docs/Secrets.md](Docs/Secrets.md))
- `Backtrace Configuration` has the settings for [https://backtrace.io/](https://backtrace.io/).
- `Blasters Environment Settings` contains API Key settings for [https://www.getjoystick.com/](https://www.getjoystick.com/).

You will also need an [Amplitude](https://amplitude.com/) API key, set as `AMPLITUDE_API_KEY` in `.env`, for tracking analytics.

## Wallet Integration with Reown WalletConnect

This project uses [Reown (formerly WalletConnect)](https://reown.com/) for blockchain wallet connectivity. To set up wallet functionality:

1. **Get your Reown Project ID:**

   - Visit [https://cloud.reown.com](https://cloud.reown.com)
   - Create a free account or sign in
   - Create a new project and copy your Project ID

2. **Add the Project ID to `.env`:**

   ```
   REOWN_PROJECT_ID=<your project id>
   ```

3. **Configure the app metadata in Unity:**
   - Navigate to `Assets/_matterless/Data/` and select `Blasters Configs`
   - In the Inspector, find the "Wallet Settings" section
   - Replace the placeholder values with your actual information:
     - **Project Name:** Your application name
     - **Project Description:** Brief description of your app
     - **Project URL:** Your project website
     - **Project Icon URL:** URL to your app icon (recommended size: 512x512px)

## NFT Integration with ERC-1155

This project includes NFT (Non-Fungible Token) integration using [Nethereum](https://nethereum.com/) and JSON-RPC fallbacks for reading ERC-1155 tokens on the Base blockchain. The implementation is read-only (no transactions), making it safe and free to use on mainnet.

1. **Get your Alchemy API Key:**

   - Visit [https://alchemy.com](https://alchemy.com)
   - Create a free account or sign in
   - Create a new App and select "Base" as the network
   - Copy your API Key from the dashboard

2. **Add the API Key to `.env`:**

   ```
   ALCHEMY_API_KEY=<your alchemy api key>
   ```

3. **Configure the contracts in Unity:**

   - Navigate to `Assets/_matterless/Data/` and select `Blasters Configs`
   - In the Inspector, find the "Chain Settings" section
   - Set the values for your deployment:
     - **NFT Contract Address:** Your ERC-1155 contract address on Base (the primary ownership check)
     - **ERC-1155 Token ID:** The token id that grants access
     - **ERC-721 Contract Address:** Optional secondary ERC-721 contract; leave the placeholder to disable
     - **RPC Endpoint:** Keep as `https://base-mainnet.g.alchemy.com/v2/`

4. **Features:**

   - Check ownership of specific token IDs (per-vehicle gating)
   - Retrieve token metadata URIs (supports `{id}` replacement and IPFS gateways)
   - Load NFT images from metadata for UI display
   - Read-only operations (no gas fees or transactions)

5. **Usage in Code:**
   ```csharp
   // NFTService is available through dependency injection
   bool owns = await nftService.OwnsToken(walletAddress, tokenId);
   string tokenURI = await nftService.GetTokenURI(tokenId);
   var sprite = await nftService.LoadNFTImage(tokenId);
   ```

## Important Security Note

**Never commit API keys to the repository.** The Auki app key and secret, the
Reown project ID, the Alchemy API key, and the Amplitude API key live only in
the gitignored `.env` file and the asset generated from it (see
[Docs/Secrets.md](Docs/Secrets.md)). The pre-push hook refuses commits that
contain them. Backtrace and Joystick settings are still stored in their config
assets, so keep those as placeholders in version control.

## Dependencies Not Included

Two paid assets are used by the official builds but are not part of this repository. The
project works without them (see [Paid Assets](#paid-assets-optional) and
[Docs/PaidAssets.md](Docs/PaidAssets.md)); if you own licenses you can drop them in:

- [Effectcore Stylized Explosion Pack 1](https://assetstore.unity.com/packages/vfx/particles/stylized-explosion-pack-1-79037) for explosion effects.
- [AVPro Movie Capture](https://assetstore.unity.com/packages/tools/video/avpro-movie-capture-mobile-edition-221852) for recording functionality.

The project also has Unity package manager dependencies to the following public repositories:

- [https://github.com/matterless/audio-module](https://github.com/matterless/audio-module)
- [https://github.com/matterless/localisation-module](https://github.com/matterless/localisation-module)
- [https://github.com/matterless/inject-module](https://github.com/matterless/inject-module)
- [https://github.com/matterless/native-share-module](https://github.com/matterless/native-share-module)
