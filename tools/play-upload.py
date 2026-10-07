#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.10"
# dependencies = [
#     "google-api-python-client>=2.100",
#     "google-auth>=2.20",
#     "google-auth-httplib2>=0.2",
# ]
# ///
"""
Upload an Android App Bundle and its native debug symbols to Google Play.

Talks to the Google Play Developer API with a service account. The package name
and the service account key come from .env (or the process environment):

    ANDROID_PACKAGE_NAME        package of the Play app, e.g. com.example.game
    PLAY_SERVICE_ACCOUNT_JSON   path to the service account's JSON key (keep it outside the repo)

    tools/play-upload.py next-version-code
        print the highest version code Play has seen, plus one

    tools/play-upload.py upload [--aab PATH] [--track internal] [--notes TEXT | --notes-file PATH]
                                [--publish] [--validate-only]
        upload the bundle (default: newest .aab in Builds/) and the matching
        *.symbols.zip next to it, then add a release to the track. The release
        is a draft unless --publish is given. --validate-only runs every step
        but throws the edit away instead of committing it.

See Docs/PlayRelease.md for the one-time service account setup.
"""

import argparse
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BUILDS = ROOT / "Builds"
SCOPES = ["https://www.googleapis.com/auth/androidpublisher"]
UPLOAD_TIMEOUT_SECONDS = 900

# Releases in these states stay on the track when a new release is added;
# older drafts are replaced by the new one.
KEPT_RELEASE_STATUSES = {"completed", "inProgress", "halted"}


def die(message):
    print(f"error: {message}", file=sys.stderr)
    sys.exit(1)


def read_env():
    """Values from .env, overridden by the process environment (same rules as SecretsSync)."""
    values = {}
    env_file = ROOT / ".env"
    if env_file.exists():
        for raw in env_file.read_text().splitlines():
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            if line.startswith("export "):
                line = line[len("export "):]
            key, sep, value = line.partition("=")
            if not sep:
                continue
            value = value.strip()
            if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
                value = value[1:-1]
            values[key.strip()] = value
    for key in set(values) | {"ANDROID_PACKAGE_NAME", "PLAY_SERVICE_ACCOUNT_JSON"}:
        if os.environ.get(key):
            values[key] = os.environ[key]
    return values


def require(env, key):
    value = env.get(key)
    if not value:
        die(f"{key} is not set in .env or the environment (see Docs/PlayRelease.md)")
    return value


def publisher_service(env):
    import httplib2
    from google.oauth2 import service_account
    from google_auth_httplib2 import AuthorizedHttp
    from googleapiclient.discovery import build

    key_path = Path(os.path.expanduser(require(env, "PLAY_SERVICE_ACCOUNT_JSON")))
    if not key_path.is_file():
        die(f"service account key not found: {key_path}")
    credentials = service_account.Credentials.from_service_account_file(str(key_path), scopes=SCOPES)
    http = AuthorizedHttp(credentials, http=httplib2.Http(timeout=UPLOAD_TIMEOUT_SECONDS))
    return build("androidpublisher", "v3", http=http, cache_discovery=False)


def explain_http_error(error):
    """Turn the Play API errors people actually hit into a next step."""
    text = str(error)
    if "Only releases with status draft may be created on draft app" in text:
        return text + "\n  The app has never been reviewed, so Play accepts drafts only: run again without --publish and roll the draft out in Play Console."
    if "already been used" in text:
        return text + "\n  Rebuild with a higher build number (tools/play-release.sh picks the next one from Play)."
    if "The caller does not have permission" in text or "403" in text:
        return text + "\n  Invite the service account in Play Console > Users and permissions and grant it release access to this app."
    return text


def newest_bundle():
    bundles = sorted(BUILDS.glob("*.aab"), key=lambda p: p.stat().st_mtime, reverse=True)
    if not bundles:
        die(f"no .aab in {BUILDS}; build one first or pass --aab")
    return bundles[0]


def symbols_for(aab, version_code):
    """Unity writes <aab stem>-<version>-v<code>-IL2CPP.symbols.zip next to the bundle."""
    matches = sorted(aab.parent.glob(f"{aab.stem}-*-v{version_code}-*.symbols.zip"))
    return matches[-1] if matches else None


def version_name_from(aab):
    """The build configs name bundles <folder>-<version>b<build>[postfix].aab."""
    match = re.search(r"-(\d[\w.]*?)b\d+", aab.stem)
    return match.group(1) if match else None


def merged_releases(existing, new_release):
    kept = [r for r in existing if r.get("status") in KEPT_RELEASE_STATUSES]
    return kept + [new_release]


def next_version_code(service, package):
    edit = service.edits().insert(packageName=package, body={}).execute()
    try:
        bundles = service.edits().bundles().list(packageName=package, editId=edit["id"]).execute()
        apks = service.edits().apks().list(packageName=package, editId=edit["id"]).execute()
    finally:
        service.edits().delete(packageName=package, editId=edit["id"]).execute()
    codes = [int(b["versionCode"]) for b in bundles.get("bundles", [])]
    codes += [int(a["versionCode"]) for a in apks.get("apks", [])]
    return max(codes, default=0) + 1


def upload(service, package, args):
    from googleapiclient.http import MediaFileUpload

    aab = Path(args.aab).resolve() if args.aab else newest_bundle()
    if not aab.is_file():
        die(f"bundle not found: {aab}")

    notes = args.notes
    if args.notes_file:
        notes = Path(args.notes_file).read_text().strip()

    edit_id = service.edits().insert(packageName=package, body={}).execute()["id"]
    committed = False
    try:
        size_mb = aab.stat().st_size / 1e6
        print(f"uploading {aab.name} ({size_mb:.0f} MB) to {package}")
        media = MediaFileUpload(str(aab), mimetype="application/octet-stream", resumable=True, chunksize=16 * 1024 * 1024)
        bundle = service.edits().bundles().upload(packageName=package, editId=edit_id, media_body=media).execute()
        version_code = int(bundle["versionCode"])
        print(f"  accepted as version code {version_code}")

        symbols = symbols_for(aab, version_code)
        if symbols:
            print(f"uploading native debug symbols {symbols.name} ({symbols.stat().st_size / 1e6:.0f} MB)")
            media = MediaFileUpload(str(symbols), mimetype="application/octet-stream", resumable=True, chunksize=16 * 1024 * 1024)
            service.edits().deobfuscationfiles().upload(
                packageName=package, editId=edit_id, apkVersionCode=version_code,
                deobfuscationFileType="nativeCode", media_body=media).execute()
        else:
            print(f"warning: no *-v{version_code}-*.symbols.zip next to the bundle; Play crash reports stay unsymbolicated", file=sys.stderr)

        version_name = version_name_from(aab)
        release = {
            "name": f"{version_code} ({version_name})" if version_name else str(version_code),
            "versionCodes": [str(version_code)],
            "status": "completed" if args.publish else "draft",
        }
        if notes:
            release["releaseNotes"] = [{"language": args.language, "text": notes}]

        track = service.edits().tracks().get(packageName=package, editId=edit_id, track=args.track).execute()
        track["releases"] = merged_releases(track.get("releases", []), release)
        service.edits().tracks().update(packageName=package, editId=edit_id, track=args.track, body=track).execute()
        print(f"release '{release['name']}' added to the {args.track} track as {release['status']}")

        if args.validate_only:
            service.edits().validate(packageName=package, editId=edit_id).execute()
            print("validated; edit discarded (--validate-only)")
            return

        service.edits().commit(packageName=package, editId=edit_id).execute()
        committed = True
        if args.publish:
            print("committed: the release is rolling out to the track's testers")
        else:
            print("committed as a draft: review it in Play Console and press Start rollout")
    finally:
        if not committed:
            try:
                service.edits().delete(packageName=package, editId=edit_id).execute()
            except Exception:
                pass


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("next-version-code", help="print the next free version code")

    up = sub.add_parser("upload", help="upload a bundle and its symbols and add a release")
    up.add_argument("--aab", help="bundle to upload (default: newest .aab in Builds/)")
    up.add_argument("--track", default="internal", help="internal, alpha, beta or production (default: internal)")
    up.add_argument("--notes", help="release notes text")
    up.add_argument("--notes-file", help="file with the release notes text")
    up.add_argument("--language", default="en-US", help="language of the release notes (default: en-US)")
    up.add_argument("--publish", action="store_true", help="roll the release out instead of leaving a draft")
    up.add_argument("--validate-only", action="store_true", help="do everything except committing the edit")

    args = parser.parse_args()
    env = read_env()
    package = require(env, "ANDROID_PACKAGE_NAME")

    from googleapiclient.errors import HttpError

    try:
        service = publisher_service(env)
        if args.command == "next-version-code":
            print(next_version_code(service, package))
        else:
            upload(service, package, args)
    except HttpError as error:
        die(explain_http_error(error))


if __name__ == "__main__":
    main()
