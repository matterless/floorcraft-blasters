The `_matterless/Data` folder contains configurations, some of which you'll need to update with your own information.

- `Blasters Configs` and `Floorcraft Configs` hold configurations for privacy policy, terms of services, Auki posemesh app key, secret and domain id
- `Backtrace Configuration` has the settings for [https://backtrace.io/](https://backtrace.io/).
- `Blasters Environment Settings` contains API Key settings for [https://www.getjoystick.com/](https://www.getjoystick.com/).

You will also need an [Amplitude](https://amplitude.com/) App key in `AnalyticsService.cs` (`m_Amplitude.init("YOUR_AMPLITUDE_ID");`) for tracking analytics.

## Wallet Integration with Reown WalletConnect

This project uses [Reown (formerly WalletConnect)](https://reown.com/) for blockchain wallet connectivity. To set up wallet functionality:

1. **Get your Reown Project ID:**
   - Visit [https://cloud.reown.com](https://cloud.reown.com)
   - Create a free account or sign in
   - Create a new project and copy your Project ID

2. **Configure in Unity:**
   - Open Unity Editor
   - Navigate to `Assets/_matterless/Data/`
   - Select `Blasters Configs`
   - In the Inspector, find the "Wallet Settings" section
   - Replace the placeholder values with your actual information:
     - **Project ID:** Your Reown Project ID from step 1
     - **Project Name:** Your application name
     - **Project Description:** Brief description of your app
     - **Project URL:** Your project website
     - **Project Icon URL:** URL to your app icon (recommended size: 512x512px)

## NFT Integration with ERC-721

This project includes NFT (Non-Fungible Token) integration using [Nethereum](https://nethereum.com/) for reading ERC-721 tokens on the Base blockchain. The implementation is read-only (no transactions), making it safe and free to use on mainnet.

1. **Get your Alchemy API Key:**
   - Visit [https://alchemy.com](https://alchemy.com)
   - Create a free account or sign in
   - Create a new App and select "Base" as the network
   - Copy your API Key from the dashboard

2. **Configure in Unity:**
   - Open Unity Editor
   - Navigate to `Assets/_matterless/Data/`
   - Select `Blasters Configs`
   - In the Inspector, find the "Chain Settings" section
   - Replace the placeholder values:
     - **NFT Contract Address:** Your ERC-721 contract address on Base blockchain (e.g., `0x1234...`)
     - **RPC Endpoint:** Keep as `https://base-mainnet.g.alchemy.com/v2/`
     - **API Key:** Your Alchemy API Key from step 1

3. **Features:**
   - Check NFT ownership by wallet address
   - Query all token IDs owned by an address
   - Retrieve token metadata URIs
   - Get contract information (name, symbol, total supply)
   - Read-only operations (no gas fees or transactions)

4. **Usage in Code:**
   ```csharp
   // NFTService is available through dependency injection
   var tokens = await nftService.GetOwnedTokenIds(walletAddress);
   var tokenURI = await nftService.GetTokenURI(tokenId);
   ```

## Important Security Note

**Never commit your actual API keys to the repository.** The config files should remain with placeholder text in version control. Only fill in real values in your local development environment. This applies to:
- Auki posemesh App Key and Secret
- Reown WalletConnect Project ID
- Alchemy API Key (for NFT/blockchain access)
- Backtrace API settings
- Joystick API Key
- Amplitude App Key

The original project had dependencies on two paid assets which were removed from the open-source version:

- [Effectcore Stylized Explosion Pack 1](https://assetstore.unity.com/packages/vfx/particles/stylized-explosion-pack-1-79037) for explosion effects.
- [AVPro Movie Capture](https://assetstore.unity.com/packages/tools/video/avpro-movie-capture-mobile-edition-221852) for recording functionality.

The project also has Unity package manager dependencies to the following repositories

- [https://github.com/matterless/audio-module](https://github.com/matterless/audio-module)
- [https://github.com/matterless/localisation-module](https://github.com/matterless/localisation-module)
- [https://github.com/matterless/inject-module](https://github.com/matterless/inject-module)
