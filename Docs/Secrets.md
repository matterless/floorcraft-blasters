# Secrets and deployment identity

API keys and deployment-specific identifiers are never committed. They live in
a `.env` file at the project root, which the Unity editor turns into a
generated `AppSecrets` asset that ships in the build. Both files are gitignored.
The tracked config assets and Player Settings only ever hold placeholders.

## Variables

### API keys

| Variable | Used by | Where to get it |
|---|---|---|
| `AUKI_APP_KEY`, `AUKI_APP_SECRET` | Auki posemesh session and DDS authentication | [Auki Console](https://console.auki.network) |
| `AMPLITUDE_API_KEY` | Analytics | [Amplitude](https://amplitude.com) |
| `REOWN_PROJECT_ID` | Wallet connection (Reown AppKit) | [Reown Cloud](https://cloud.reown.com) |
| `ALCHEMY_API_KEY` | NFT ownership checks on Base | [Alchemy](https://alchemy.com) |

### Deployment identity (optional)

| Variable | Used by |
|---|---|
| `AUKI_DOMAIN_ID` | Posemesh domain the app joins by default (`AukiSettings.appDomainId`) |
| `AUKI_EDITOR_SESSION_ID` | Editor only: session to connect to when "Use This Session Id In Editor" is ticked |

### Android store builds

Read only by the Android build configuration (`BC_*` assets with Platform = Android),
never by the runtime:

| Variable | Used by |
|---|---|
| `ANDROID_PACKAGE_NAME` | Package name of the store build; the tracked Player Settings keep a neutral placeholder |
| `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_ALIAS` | Release keystore; without them the build is debug-signed, which Google Play rejects |
| `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_PASS` | Keystore passwords; optional, Unity also accepts them typed into Publishing Settings per editor session |

## Setup

```bash
cp .env.example .env
# fill in the values
tools/paid-assets.sh hook   # optional: installs the pre-push guard described below
```

`.env` is a plain `KEY=VALUE` file. Blank lines and `#` comments are ignored,
values may be quoted, and a leading `export ` is accepted.

Open the project in Unity. `SecretsSync` (an editor script in
`Assets/_matterless/Scripts/Editor`) reads `.env` whenever scripts reload,
before every build, and from **Matterless > Secrets > Sync from .env**, and
writes `Assets/_matterless/Resources/AppSecrets.asset`. The console logs
`[Secrets] ... updated from .env` when the asset changes. Process environment
variables with the same names override `.env`, which is how CI supplies them.

## How the values reach the code

At runtime `RootInstaller` calls `AppSecrets.Load()`, which loads the asset from
`Resources`, and passes it to `AppConfigs.ApplySecrets`. That pushes each value
into a non-serialized field of the settings object that needs it:

| Value | Settings object | Accessor |
|---|---|---|
| Auki app key, secret, domain id, editor session id | `AukiSettings` | `appKey`, `appSecret`, `appDomainId`, `sessionId` |
| Amplitude API key | `AnalyticsSettings` | `amplitudeApiKey` |
| Reown project id | `WalletSettings` | `projectId` |
| Alchemy API key | `ChainSettings` | `apiKey`, `rpcUrl` |

Because the receiving fields are not serialized, nothing is ever written back
into the tracked config assets, and services keep reading the same accessors
they always did.

For Android store builds, the build configuration applies the package name and
keystore from `.env` to Player Settings for the duration of the build and
restores the neutral values afterwards, so a Play build never dirties the
tracked settings. Build And Run from Build Settings keeps working for device
tests under the neutral package name.

## Without secrets

The project compiles and runs. `AppSecrets.Load()` logs one error naming this
document, and each feature that needs a key logs its own warning and stays
disabled: no Auki session, no analytics, no wallet, no NFT checks. Without the
Android variables, builds use the neutral package name and the debug keystore.

## Guard rails

- `.env`, `AppSecrets.asset`, and its meta file are gitignored.
- The pre-push hook installed by `tools/paid-assets.sh hook` refuses to push a
  commit that contains those files, a config asset where one of the former
  key fields still carries a value, or a tree whose settings carry the store
  package name, a posemesh domain or session id, or a keystore path. Run
  `tools/paid-assets.sh check` to scan unpushed commits by hand.

## Adding a new secret

1. Add the variable to `.env.example` and to your `.env`.
2. Add a serialized field and accessor to `AppSecrets`, and the mapping in
   `SecretsSync.s_Keys`.
3. Give the consuming settings class a non-serialized field plus a `SetSecrets`
   method, and wire it in `AppConfigs.ApplySecrets`.
4. If the value previously lived in a config asset, add its field name to
   `SECRET_FIELDS` or `IDENTITY_PATTERNS` in `tools/githooks/pre-push`.
