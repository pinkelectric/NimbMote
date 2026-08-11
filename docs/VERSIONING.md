# Versioning and releases

Bentley Remote follows Semantic Versioning. The canonical version is stored in
the root `VERSION` file as `MAJOR.MINOR.PATCH`; Git tags add the `v` prefix.

## Version mapping

- Android `versionName` is the exact SemVer value from `VERSION`.
- Android `versionCode` is calculated as
  `MAJOR * 1,000,000 + MINOR * 1,000 + PATCH + 1`.
- Windows `Version` and `InformationalVersion` use the exact SemVer value.
- Windows assembly and file versions append a fourth zero component.

## Release sequence

1. Confirm the preceding working state is committed with `git status` and
   `git log -1`.
2. Update `VERSION` according to SemVer and make the intended changes.
3. Build and verify the Android APK and the self-contained Windows x64 agent.
4. Create `releases/vX.Y.Z/` containing only the versioned APK, Windows ZIP and
   `CHANGELOG.md` with checksums.
5. Commit the source, release metadata and artifacts as one release commit.
6. Create an annotated `vX.Y.Z` tag on that commit.
7. Confirm the tag target and a clean working tree before starting new work.

The release directory is a distribution shelf, not a source archive. Git is the
only versioned history of the source tree.
