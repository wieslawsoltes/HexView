---
title: "Lunet Docs Pipeline"
---

# Lunet Docs Pipeline

This repository now uses the same Lunet-based docs pattern as the TreeDataGrid repository.

## Site Structure

- `site/config.scriban`: Lunet config and API docs setup
- `site/menu.yml`: top-level navigation
- `site/readme.md`: home page
- `site/api-src/HexView.ApiDocs.csproj`: docs-only project used for API extraction from the HexView sources
- `site/articles/**`: authored documentation pages
- `site/articles/**/menu.yml`: sidebar navigation groups
- `site/.lunet/css/template-main.css`: precompiled template stylesheet
- `site/.lunet/css/site-overrides.css`: HexView-specific styling
- `site/.lunet/includes/_builtins/bundle.sbn-html`: bundle override
- `site/.lunet/layouts/**`: custom API layouts

## Commands

From repository root:

```bash
./build-docs.sh
./check-docs.sh
./serve-docs.sh
```

PowerShell:

```powershell
./build-docs.ps1
./serve-docs.ps1
```

## API Generation

The API reference is generated via `with api.dotnet` in `site/config.scriban` from:

- `site/api-src/HexView.ApiDocs.csproj`

## CI

GitHub Actions publishes the built site from `site/.lunet/build/www` using `.github/workflows/docs.yml`.
