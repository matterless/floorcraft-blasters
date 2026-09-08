#!/usr/bin/env bash
#
# Manage the paid Unity assets that must never be committed to this repository.
#
# The assets live in private git repositories and are cloned straight into
# their Assets/Plugins/ locations, which are gitignored here. The AVPro
# integration code compiles only when the AVPRO_MOVIECAPTURE define is set,
# which this script provides through a gitignored csc.rsp next to the main
# assembly definition.
#
#   tools/paid-assets.sh install   clone (or update) the paid assets, enable AVPro, install push guard
#   tools/paid-assets.sh remove    delete the paid assets and disable AVPro (open-source state)
#   tools/paid-assets.sh status    show what is installed
#   tools/paid-assets.sh check     verify that no paid asset is in commits about to be pushed
#   tools/paid-assets.sh hook      (re)install the pre-push guard only
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Private repositories holding the paid assets. Override with environment
# variables if the repositories move.
AVPRO_REPO="${AVPRO_REPO:-git@github.com:tomicz/avpro-movie-capture.git}"
EFFECTCORE_REPO="${EFFECTCORE_REPO:-git@github.com:tomicz/effectcore-explosions.git}"

AVPRO_DIR="$ROOT/Assets/Plugins/RenderHeads"
EFFECTCORE_DIR="$ROOT/Assets/Plugins/EffectCore"
RSP="$ROOT/Assets/_matterless/Scripts/Runtime/csc.rsp"
HOOK_SRC="$ROOT/tools/githooks/pre-push"
HOOK_DST="$ROOT/.git/hooks/pre-push"

log() { printf '%s\n' "$*"; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }

clone_or_update() {
    local repo="$1" dir="$2" name="$3"
    if [ -d "$dir/.git" ]; then
        log "updating $name in ${dir#$ROOT/}"
        git -C "$dir" pull --ff-only
    elif [ -e "$dir" ]; then
        die "${dir#$ROOT/} exists but is not a git checkout; move it away or delete it first"
    else
        log "cloning $name into ${dir#$ROOT/}"
        git clone "$repo" "$dir"
    fi
}

refuse_if_dirty() {
    local dir="$1" name="$2"
    [ -d "$dir/.git" ] || return 0
    if [ -n "$(git -C "$dir" status --porcelain)" ]; then
        die "$name has uncommitted changes in ${dir#$ROOT/}; commit or discard them, or pass --force"
    fi
    if [ -n "$(git -C "$dir" log --branches --not --remotes --oneline 2>/dev/null)" ]; then
        die "$name has unpushed commits in ${dir#$ROOT/}; push them first, or pass --force"
    fi
}

install_hook() {
    [ -d "$ROOT/.git" ] || return 0
    cp "$HOOK_SRC" "$HOOK_DST"
    chmod +x "$HOOK_DST"
    log "pre-push guard installed at .git/hooks/pre-push"
}

cmd_install() {
    clone_or_update "$AVPRO_REPO" "$AVPRO_DIR" "AVPro Movie Capture"
    clone_or_update "$EFFECTCORE_REPO" "$EFFECTCORE_DIR" "EffectCore explosions"
    printf -- '-define:AVPRO_MOVIECAPTURE\n' > "$RSP"
    log "AVPRO_MOVIECAPTURE enabled via ${RSP#$ROOT/}"
    install_hook
    log "done. Let Unity reimport, then verify the record button works."
}

cmd_remove() {
    local force=0
    [ "${1:-}" = "--force" ] && force=1
    if [ "$force" -eq 0 ]; then
        refuse_if_dirty "$AVPRO_DIR" "AVPro Movie Capture"
        refuse_if_dirty "$EFFECTCORE_DIR" "EffectCore explosions"
    fi
    rm -rf "$AVPRO_DIR" "$AVPRO_DIR.meta" "$EFFECTCORE_DIR" "$EFFECTCORE_DIR.meta"
    rm -f "$RSP" "$RSP.meta"
    log "paid assets removed; project is now in its open-source state"
}

cmd_status() {
    local d
    for d in "$AVPRO_DIR" "$EFFECTCORE_DIR"; do
        if [ -d "$d/.git" ]; then
            printf '%-32s installed (%s)\n' "${d#$ROOT/}" "$(git -C "$d" rev-parse --short HEAD)"
        elif [ -d "$d" ]; then
            printf '%-32s present but not a git checkout\n' "${d#$ROOT/}"
        else
            printf '%-32s not installed\n' "${d#$ROOT/}"
        fi
    done
    if [ -f "$RSP" ]; then
        printf '%-32s %s\n' "AVPRO_MOVIECAPTURE" "enabled"
    else
        printf '%-32s %s\n' "AVPRO_MOVIECAPTURE" "disabled (DummyRecordingService is used)"
    fi
    if [ -f "$HOOK_DST" ] && cmp -s "$HOOK_SRC" "$HOOK_DST"; then
        printf '%-32s %s\n' "pre-push guard" "installed"
    else
        printf '%-32s %s\n' "pre-push guard" "NOT installed (run: tools/paid-assets.sh hook)"
    fi
}

cmd_check() {
    # Default range: everything on the current branch that is not on any remote yet.
    local range="${1:-HEAD --not --remotes}"
    # shellcheck disable=SC2086
    "$HOOK_SRC" --range $range
}

case "${1:-}" in
    install) cmd_install ;;
    remove)  cmd_remove "${2:-}" ;;
    status)  cmd_status ;;
    check)   shift; cmd_check "$@" ;;
    hook)    install_hook ;;
    *)       sed -n '2,15p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
esac
