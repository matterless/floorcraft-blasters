#!/usr/bin/env bash
#
# Build the Android store bundle and send it to Google Play in one go.
#
#   1. asks Play for the next free version code (or takes --build-number)
#   2. builds BC_Floorcraft_Blasters_Android in Unity batchmode with that build number
#   3. checks the bundle: release-signed with the .env keystore, Java plugin classes present, symbols zip present
#   4. uploads bundle and symbols with tools/play-upload.py and adds a release
#
#   tools/play-release.sh [--notes TEXT | --notes-file PATH] [--track internal] [--publish]
#                         [--build-number N] [--config NAME] [--skip-upload] [--validate-only]
#
# The release stays a draft unless --publish is given. The Unity editor must be
# closed: batchmode cannot open a project that is already open. See Docs/PlayRelease.md.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UPLOAD="$ROOT/tools/play-upload.py"
LOG="$ROOT/Logs/play-release-build.log"

CONFIG="BC_Floorcraft_Blasters_Android"
BUILD_NUMBER=""
SKIP_UPLOAD=0
UPLOAD_ARGS=()

log() { printf '%s\n' "$*"; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }
usage() { sed -n '2,15p' "$0" | sed 's/^# \{0,1\}//'; exit "${1:-1}"; }

# Value of KEY from the process environment, else from .env (unquoted).
env_value() {
    local key="$1"
    if [ -n "${!key:-}" ]; then
        printf '%s' "${!key}"
        return
    fi
    [ -f "$ROOT/.env" ] || return 0
    sed -n "s/^[[:space:]]*\(export[[:space:]]\{1,\}\)\{0,1\}$key[[:space:]]*=[[:space:]]*//p" "$ROOT/.env" \
        | tail -n 1 | sed "s/^[\"']\(.*\)[\"']\$/\1/"
}

while [ $# -gt 0 ]; do
    case "$1" in
        --notes|--notes-file|--track)
            [ $# -ge 2 ] || die "$1 needs a value"
            UPLOAD_ARGS+=("$1" "$2"); shift 2 ;;
        --publish|--validate-only)
            UPLOAD_ARGS+=("$1"); shift ;;
        --build-number)
            [ $# -ge 2 ] || die "$1 needs a value"
            BUILD_NUMBER="$2"; shift 2 ;;
        --config)
            [ $# -ge 2 ] || die "$1 needs a value"
            CONFIG="$2"; shift 2 ;;
        --skip-upload)
            SKIP_UPLOAD=1; shift ;;
        -h|--help)
            usage 0 ;;
        *)
            die "unknown option: $1 (see --help)" ;;
    esac
done

command -v uv >/dev/null || die "uv is required to run tools/play-upload.py (brew install uv)"

UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"
[ -x "$UNITY" ] || die "Unity $UNITY_VERSION not found at $UNITY (set UNITY to override)"

if pgrep -if "Unity.app/Contents/MacOS/Unity .*-projectpath $ROOT( |\$)" >/dev/null; then
    die "the project is open in the Unity editor; close it first (batchmode cannot open it twice)"
fi

# 1. version code
if [ -z "$BUILD_NUMBER" ]; then
    log "asking Google Play for the next version code"
    BUILD_NUMBER="$(uv run --quiet --script "$UPLOAD" next-version-code)" || die "could not read version codes from Play"
fi
[[ "$BUILD_NUMBER" =~ ^[1-9][0-9]*$ ]] || die "build number must be a positive integer, got '$BUILD_NUMBER'"
log "version code $BUILD_NUMBER"

# 2. build
mkdir -p "$(dirname "$LOG")"
log "building $CONFIG in Unity $UNITY_VERSION (log: ${LOG#$ROOT/})"
set +e
CUSTOM_PARAMETERS="-configName $CONFIG -buildNumber $BUILD_NUMBER" \
    "$UNITY" -batchmode -quit -projectPath "$ROOT" -buildTarget Android \
    -executeMethod Matterless.Floorcraft.Editor.BuilderForCI.Build -logFile "$LOG"
STATUS=$?
set -e
if [ $STATUS -ne 0 ]; then
    grep -E "error CS|Build failed|BuildFailedException|FAILURE:|\[Build\]" "$LOG" | tail -n 20 >&2 || true
    die "Unity build failed (exit $STATUS); see ${LOG#$ROOT/}"
fi

# 3. checks
AAB="$(sed -n 's/^Build output: //p' "$LOG" | tail -n 1)"
[ -n "$AAB" ] && [ -f "$AAB" ] || die "the build succeeded but its output path is not in ${LOG#$ROOT/}"
[[ "$AAB" == *.aab ]] || die "$CONFIG produced ${AAB#$ROOT/}, not an app bundle; Play needs a .aab"
log "built ${AAB#$ROOT/}"

JDK_BIN="$(dirname "$UNITY")/../../../PlaybackEngines/AndroidPlayer/OpenJDK/bin"
KEYTOOL="$JDK_BIN/keytool"
[ -x "$KEYTOOL" ] || KEYTOOL="keytool"

SIGNED_BY="$("$KEYTOOL" -printcert -jarfile "$AAB" 2>/dev/null | sed -n 's/^[[:space:]]*SHA256: //p' | head -n 1)"
[ -n "$SIGNED_BY" ] || die "${AAB#$ROOT/} is not signed"
if "$KEYTOOL" -printcert -jarfile "$AAB" 2>/dev/null | grep -q "CN=Android Debug"; then
    die "${AAB#$ROOT/} is debug-signed; set the ANDROID_KEYSTORE_* values in .env"
fi

KEYSTORE="$(env_value ANDROID_KEYSTORE_PATH)"
ALIAS="$(env_value ANDROID_KEYSTORE_ALIAS)"
STORE_PASS="$(env_value ANDROID_KEYSTORE_PASS)"
if [ -n "$KEYSTORE" ] && [ -n "$ALIAS" ] && [ -n "$STORE_PASS" ]; then
    EXPECTED="$(PLAY_RELEASE_STORE_PASS="$STORE_PASS" "$KEYTOOL" -list -keystore "${KEYSTORE/#\~/$HOME}" \
        -storepass:env PLAY_RELEASE_STORE_PASS -alias "$ALIAS" 2>/dev/null \
        | sed -n 's/^Certificate fingerprint (SHA-256): //p')"
    [ "$SIGNED_BY" = "$EXPECTED" ] || die "${AAB#$ROOT/} is not signed with the $ALIAS key from $KEYSTORE"
    log "signed with the release key '$ALIAS'"
else
    log "warning: keystore passwords not in .env, so the signer was only checked to not be the debug key"
fi

# The Android Resolver keeps the Java libraries in a gitignored folder, so a build without them still
# succeeds; the app then crashes at startup when C# looks the plugin class up by name.
for CLASS in com/amplitude/unity/plugins/AmplitudePlugin; do
    FOUND="$(unzip -p "$AAB" 'base/dex/*.dex' | LC_ALL=C grep -a -c "L$CLASS;" || true)"
    [ "${FOUND:-0}" -gt 0 ] || die "${AAB#$ROOT/} has no $CLASS; run Android Resolver > Force Resolve in Unity and build again"
done
log "Java plugin classes present"

SYMBOLS="$(find "$ROOT/Builds" -maxdepth 1 -name "$(basename "$AAB" .aab)-*-v$BUILD_NUMBER-*.symbols.zip" | head -n 1)"
[ -n "$SYMBOLS" ] || die "no native debug symbols zip for version code $BUILD_NUMBER next to the bundle"

# Unity keeps an unchanged symbols zip from an earlier build, so match it by ELF build ID, not by name.
READELF="$(ls "$(dirname "$UNITY")"/../../../PlaybackEngines/AndroidPlayer/NDK/toolchains/llvm/prebuilt/*/bin/llvm-readelf 2>/dev/null | head -n 1)"
if [ -n "$READELF" ]; then
    CHECK_DIR="$ROOT/Logs/play-release-symbols-check"
    rm -rf "$CHECK_DIR"; mkdir -p "$CHECK_DIR"
    unzip -q "$AAB" 'base/lib/*/libil2cpp.so' -d "$CHECK_DIR/bundle"
    unzip -q "$SYMBOLS" '*/libil2cpp.so' -d "$CHECK_DIR/symbols"
    for LIB in "$CHECK_DIR"/bundle/base/lib/*/libil2cpp.so; do
        ABI="$(basename "$(dirname "$LIB")")"
        BUNDLE_ID="$("$READELF" -n "$LIB" | awk '/Build ID/ {print $3}')"
        SYMBOLS_ID="$("$READELF" -n "$CHECK_DIR/symbols/$ABI/libil2cpp.so" 2>/dev/null | awk '/Build ID/ {print $3}')"
        [ -n "$BUNDLE_ID" ] && [ "$BUNDLE_ID" = "$SYMBOLS_ID" ] \
            || die "${SYMBOLS#$ROOT/} does not match the $ABI libil2cpp.so in the bundle"
    done
    rm -rf "$CHECK_DIR"; mkdir -p "$CHECK_DIR"
else
    log "warning: llvm-readelf not found in the Unity NDK, so the symbols were matched by file name only"
fi
log "native debug symbols ${SYMBOLS#$ROOT/}"

if [ $SKIP_UPLOAD -eq 1 ]; then
    log "skipping the upload (--skip-upload)"
    exit 0
fi

# 4. upload
uv run --quiet --script "$UPLOAD" upload --aab "$AAB" "${UPLOAD_ARGS[@]+"${UPLOAD_ARGS[@]}"}"
