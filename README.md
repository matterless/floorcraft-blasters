The `_matterless/Data` folder contains configurations, some of which you'll need to update with your own information.
- `Blasters Configs` and `Floorcraft Configs` hold configurations for privacy policy, terms of services, Auki posemesh app key, secret and domain id
- `Backtrace Configuration` has the settings for [https://backtrace.io/](https://backtrace.io/).
- `Blasters Environment Settings` contains API Key settings for [https://www.getjoystick.com/](https://www.getjoystick.com/).

You will also need an [Amplitude](https://amplitude.com/) App key in `AnalyticsService.cs` (`m_Amplitude.init("YOUR_AMPLITUDE_ID");`) for tracking analytics.  

The original project had dependencies on two paid assets which were removed from the open-source version:
- [Effectcore Stylized Explosion Pack 1](https://assetstore.unity.com/packages/vfx/particles/stylized-explosion-pack-1-79037) for explosion effects.
- [AVPro Movie Capture](https://assetstore.unity.com/packages/tools/video/avpro-movie-capture-mobile-edition-221852) for recording functionality.

The project also has Unity package manager dependencies to the following repositories
- [https://github.com/matterless/audio-module](https://github.com/matterless/audio-module)
- [https://github.com/matterless/localisation-module](https://github.com/matterless/localisation-module)
- [https://github.com/matterless/inject-module](https://github.com/matterless/inject-module)