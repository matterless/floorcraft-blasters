# Paid assets

Two paid Unity assets are used by the official iOS and Android builds but are
**not** part of this open-source repository and must never be committed to it:

| Asset | Location in project | What it provides |
|---|---|---|
| RenderHeads AVPro Movie Capture | `Assets/Plugins/RenderHeads/` | In-game video recording (`RecordingService`) |
| EffectCore Stylized Explosion Pack | `Assets/Plugins/EffectCore/` | Explosion and smoke materials used by vehicle and enemy prefabs |

Both locations are gitignored. Each asset lives in its own private git
repository and is cloned directly into place by `tools/paid-assets.sh`.

## Without the paid assets

The project opens, compiles, and builds without them:

- Recording is replaced by `DummyRecordingService`, so the record and
  screenshot buttons do nothing.
- Prefabs that reference EffectCore materials render those particles with
  missing (pink) materials. Bring your own VFX assets if you need them.

## With the paid assets (licensed developers)

You need read access to the private repositories. Then:

```bash
tools/paid-assets.sh install
```

This clones or updates both repositories, writes
`Assets/_matterless/Scripts/Runtime/csc.rsp` with `-define:AVPRO_MOVIECAPTURE`
so the AVPro-backed `RecordingService` compiles, and installs a pre-push hook
that refuses to push any commit containing the paid assets or secrets
(see [Secrets.md](Secrets.md)).

Other commands:

```bash
tools/paid-assets.sh status    # what is installed
tools/paid-assets.sh remove    # back to the open-source state (refuses if the paid repos have unsaved work)
tools/paid-assets.sh check     # scan unpushed commits for paid assets
tools/paid-assets.sh hook      # reinstall the pre-push guard
```

## Editing the paid assets

Changes made inside `Assets/Plugins/RenderHeads` or `Assets/Plugins/EffectCore`
(including Unity-generated `.meta` changes) belong to the private repositories.
Commit and push them from inside that folder:

```bash
cd Assets/Plugins/EffectCore
git add -A && git commit -m "..." && git push
```

The private repository URLs are set at the top of `tools/paid-assets.sh` and can
be overridden with the `AVPRO_REPO` and `EFFECTCORE_REPO` environment variables.

## Adding more restricted assets

1. Put the asset in its own private repository, including all `.meta` files so
   GUIDs and import settings are preserved.
2. Add its path (and the folder `.meta`) to `.gitignore` under "Paid plugins".
3. Add it to `tools/paid-assets.sh` and to `FORBIDDEN_PATHS` in `tools/githooks/pre-push`.
4. Keep any code that depends on it behind a define provided by the `csc.rsp`.
