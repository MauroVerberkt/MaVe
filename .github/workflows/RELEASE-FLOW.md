## Release flow (internal)

This file is intentionally stored under `.github/workflows/` so it is **not** part of the Docusaurus docs site.

### Stable release trigger

- Push tag: `v*` (example: `v1.2.3`)
- Workflow: `.github/workflows/release.yml`

### What the workflow enforces

1. `build-and-test` runs first and uploads tested `packages` artifact (`.nupkg` + `.snupkg`).
2. `verify-tag-on-main` checks:
   - tagged commit is on `main` lineage
   - release tag version matches **every** built `.nupkg` version in the artifact
3. Only after verification passes, `publish-nuget` publishes those exact tested packages.
4. `create-github-release` creates GitHub release notes.

### Why artifact-based version verification

Version is validated from the actual built `.nupkg` filenames rather than recomputing from git metadata.
This guarantees the published stable packages are exactly the tested artifacts and that tag/version alignment is enforced at publish time.

### Release operator checklist

1. Bump package versions (`version.json`) for the intended release.
2. Merge to `main`.
3. Tag the intended release commit as `vX.Y.Z`.
4. Push the tag.

If any package version does not equal `X.Y.Z`, release workflow fails in `verify-tag-on-main`.
