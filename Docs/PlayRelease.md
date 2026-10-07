# Google Play releases

`tools/play-release.sh` builds the Android store bundle and sends it to Google Play
in one command:

1. asks Play for the next free version code
2. builds `BC_Floorcraft_Blasters_Android` in Unity batchmode with that build number.
   The number is used for this build only: store version codes stay out of the tracked
   config and Player Settings
3. checks that the bundle is signed with the `.env` release key, that it contains the
   Java plugin classes (see [Android Java dependencies](#android-java-dependencies)) and
   that the native debug symbols zip was written
4. uploads the bundle and the symbols with `tools/play-upload.py` and adds a release to
   the internal testing track

```bash
tools/play-release.sh --notes "Fixed the stop button getting stuck"
```

The release is created as a **draft**; open Play Console > Test and release > Testing >
Internal testing and press **Start rollout**. Pass `--publish` to roll it out straight
away. Play refuses that until the app has been through its first review, and the
script says so.

| Option | Effect |
|---|---|
| `--notes TEXT`, `--notes-file PATH` | Release notes (en-US unless `tools/play-upload.py --language` is used directly) |
| `--track NAME` | `internal` (default), `alpha` (closed), `beta` (open) or `production` |
| `--publish` | Roll the release out instead of leaving a draft |
| `--validate-only` | Upload and validate everything, then discard the edit |
| `--build-number N` | Use this version code instead of asking Play |
| `--skip-upload` | Build and check only |
| `--config NAME` | Another build config under `Assets/_matterless/_BuildConfigs` |

Close the Unity editor first: batchmode cannot open a project that is already open.
The build log goes to `Logs/play-release-build.log`.

To upload a bundle that already exists, call the uploader directly:

```bash
tools/play-upload.py upload --aab Builds/Floorcraft_Blasters_Android-0.1b4.aab --notes "..."
tools/play-upload.py next-version-code
```

Both scripts need [uv](https://docs.astral.sh/uv/) (`brew install uv`), which fetches
the Google API client on first run.

## Native debug symbols

Store builds write `<bundle>-<version>-v<code>-IL2CPP.symbols.zip` next to the `.aab`
(debug-signed test builds do not). The uploader sends it with every release, so Play
Console can symbolicate native crashes and ANRs. If you upload a bundle by hand, add the
zip under App bundle explorer > the version > Downloads > Native debug symbols.

## Android Java dependencies

The Android Resolver (Assets > External Dependency Manager) downloads the Java libraries,
such as Amplitude's, into `Assets/Plugins/Android`, which is gitignored. If they are
deleted, a build still succeeds but the app crashes at startup with a
`ClassNotFoundException`. `AndroidDependencyCheck` therefore runs before every Android
build: when a required class is missing it forces a resolve, and it fails the build if
the class is still missing. `tools/play-release.sh` checks the built bundle again.

The resolver's automatic resolve is turned off (`ProjectSettings/GvhProjectSettings.xml`).
It ran on editor startup after every store build, deleted the libraries first, and then
failed because it found no usable Java. `AndroidDependencyCheck` also points the editor at
Unity's bundled JDK, so Force Resolve works whatever `JAVA_HOME` the shell sets.

## One-time setup: service account

The scripts sign in as a Google Cloud service account that has release access to the
app in Play Console. Adding it needs a Play Console admin of the developer account.

1. In [Google Cloud Console](https://console.cloud.google.com), pick (or create) a project
   and enable the **Google Play Android Developer API**.
2. IAM & Admin > Service accounts > **Create service account** (for example
   `play-release`). It needs no Cloud roles.
3. Open the service account > Keys > Add key > **JSON**. Store the downloaded file
   outside the repository, next to the release keystore. If key creation is blocked,
   the organisation policy `iam.disableServiceAccountKeyCreation` has to allow it for
   this project.
4. In [Play Console](https://play.google.com/console) > Users and permissions >
   **Invite new users**, enter the service account's e-mail address. Under App
   permissions add the app and grant **Release apps to testing tracks** (plus
   **Release to production** if the script should ever publish there). Send the invite;
   service accounts accept automatically.
5. Put the key's path into `.env`:

   ```
   PLAY_SERVICE_ACCOUNT_JSON=/path/outside/the/repo/play-release.json
   ```

   `ANDROID_PACKAGE_NAME` must match the Play app. Neither value reaches the
   `AppSecrets` asset or the build.

Check the setup with `tools/play-upload.py next-version-code`. New permissions can take
a few minutes to apply; until then Play answers with a 403.
