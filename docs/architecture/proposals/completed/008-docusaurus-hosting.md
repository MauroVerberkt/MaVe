---
sidebar_position: 8
title: "PROP-008: Docusaurus Hosting"
tags: [docs]
---

# PROP-008: Docusaurus Hosting

**Status:** done  
**Size:** small  
**Created:** 2025-05-25  
**Updated:** 2026-05-28

## Problem / Motivation

The Docusaurus documentation site currently only runs locally during development. To make the documentation accessible to consumers of the NuGet packages (and as a professional portfolio piece), the site needs to be hosted somewhere publicly accessible.

Key considerations:
- Cost (ideally free or near-free for an open-source/personal project)
- Custom domain support
- Automated deployments (CI/CD on push to main)
- SSL/HTTPS out of the box
- Minimal maintenance overhead

## Sketch

### Decision: GitHub Pages

GitHub Pages is the clear winner for this project:

- **Zero cost**, zero new vendor accounts — everything stays in GitHub (code, packages, CI, docs)
- **First-class Docusaurus support** — official deployment guide, works with `actions/deploy-pages`
- **HTTPS out of the box** on `*.github.io`
- **Minimal maintenance** — static files served from a CDN, no infrastructure to manage

PR preview deployments are sacrificed, but `npm start` locally is sufficient for a single-maintainer project. If needed later, switching to Vercel/Netlify is trivial (static site, no lock-in).

### Deployment Workflow

```yaml
# .github/workflows/deploy-docs.yml
name: Deploy Docs

on:
  push:
    branches: [main]
    paths: ['docs/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    permissions:
      pages: write
      id-token: write
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: docs/package-lock.json
      - run: npm ci
        working-directory: docs
      - run: npm run build
        working-directory: docs
      - uses: actions/upload-pages-artifact@v3
        with:
          path: docs/build
      - id: deployment
        uses: actions/deploy-pages@v4
```

### Docusaurus Configuration

Set `url` and `baseUrl` in `docusaurus.config.js`:

```js
url: 'https://mauroverberkt.github.io',
baseUrl: '/MaVe/',
```

### Repo Settings Required

- Settings → Pages → Source: "GitHub Actions" (not "Deploy from a branch")

### Post-Deploy Tasks

- Update root `README.md` docs link from "run locally" instructions to the hosted URL (`https://mauroverberkt.github.io/MaVe/`)
- Investigate build-time coverage injection: CI test run → extract percentage → inject into Docusaurus build as a custom field (enables live coverage display on landing page without client-side API calls)

## Open Questions

- ~~Do we want PR preview deployments, or is local preview sufficient?~~ **Local preview is sufficient.** Single maintainer, `npm start` works fine.
- ~~Is a custom domain planned, or is `*.github.io` / `*.vercel.app` acceptable?~~ **`*.github.io` is acceptable.** Custom domain can be added later without changing the workflow.
- ~~Should the docs site be in the same repo (monorepo) or split out?~~ **Same repo.** Docs live next to the code they document — easier to keep in sync.
- ~~Any preference for a provider that aligns with the .NET ecosystem (e.g., Azure)?~~ **No.** GitHub Pages aligns with everything else in the repo (GitHub Packages, GitHub Actions). Azure adds a vendor for no benefit.

## Stretch Goal: Integrated Project Health Dashboard

Once PROP-009 (CI pipeline) is producing test/coverage data, the docs site can surface that data directly rather than just linking to external tools.

### Option A: Embedded Health Component

A custom React component within the existing Docusaurus site that displays a summary of project health:

- Coverage %, trend sparkline, test count, last green build
- Rendered on the docs landing page or a dedicated "Project Health" page
- "View full report →" link to Codecov/SonarCloud for drill-down

**Data flow:**
```
CI run → publishes test-summary.json artifact
Docusaurus build → fetches artifact at build time (no CORS, no client-side fetch)
CI triggers docs redeploy after test data updates → always in sync
```

This keeps the docs site as the single entry point while leveraging external tools for the heavy lifting. Low effort, high portfolio value.

### Option B: Full Dashboard Page

A dedicated `/dashboard` route in Docusaurus with multiple panels:

- Coverage breakdown per project
- Benchmark history (BenchmarkDotNet trend charts)
- Build health (duration, flaky test detection)
- Branch comparison

Essentially a lightweight Grafana alternative built as Docusaurus pages. More impressive as a portfolio piece but significantly more work — likely a project in itself.

### Shared Dependency

Both options require PROP-009 to publish a machine-readable summary artifact (e.g., `test-summary.json`) as part of CI output. That's PROP-009's interface contract to this stretch goal.

## Prior Art / References

- [Docusaurus Deployment docs](https://docusaurus.io/docs/deployment)
- [GitHub Pages guide for Docusaurus](https://docusaurus.io/docs/deployment#deploying-to-github-pages)
- [Vercel Docusaurus template](https://vercel.com/templates/docusaurus)
- [Netlify Docusaurus guide](https://docs.netlify.com/frameworks/docusaurus/)

## Outcome

Done. GitHub Pages deployment implemented:

- `.github/workflows/deploy-docs.yml` deploys on push to `main` when `docs/**` changes
- `docusaurus.config.ts` configured with correct `url` and `baseUrl`
- GitHub repo settings configured: Pages source set to "GitHub Actions"
- README updated with link to hosted site

Site live at: https://mauroverberkt.github.io/MaVe/

Stretch goal (project health dashboard) can now be pursued — PROP-009 (CI pipeline) is complete.
