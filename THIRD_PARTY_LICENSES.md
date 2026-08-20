# Third Party Licenses

> **Last Updated:** 2026-08-20

This document tracks all third-party components used in UniversalRPG.

## Current Third-Party Components

### Noto Sans CJK SC

- **Component:** `assets/fonts/NotoSansCJKsc-Regular.otf`
- **Purpose:** Bundled Latin, Japanese, Korean, and Simplified Chinese UI glyph coverage
- **Source:** [notofonts/noto-cjk](https://github.com/notofonts/noto-cjk)
- **License:** SIL Open Font License 1.1
- **License text:** [`assets/fonts/OFL.txt`](assets/fonts/OFL.txt)

## Planned Third-Party Components

The following components are **planned** but not yet included:

### Ruby VM (for RGSS support)

**Candidates:**
- **mruby** — MIT License
  - Small footprint, embeddable Ruby implementation
  - Suitable for RGSS1/2/3 compatibility
  - [https://mruby.org/](https://mruby.org/)

- **Rubinius** — Apache License 2.0
  - More complete Ruby implementation
  - Larger footprint
  - [https://rubini.us/](https://rubini.us/)

**Decision pending:** Will be evaluated based on RGSS compatibility requirements.

### JavaScript Engine (for MV/MZ support)

**Candidates:**
- **QuickJS** — MIT License
  - Small footprint (~300KB)
  - Good ES2020 support
  - [https://bellard.org/quickjs/](https://bellard.org/quickjs/)

- **Duktape** — MIT License
  - Very small footprint
  - Good embeddability
  - [https://duktape.org/](https://duktape.org/)

- **V8** — BSD-style
  - Full Chrome V8 engine
  - Largest footprint, best compatibility
  - [https://v8.dev/](https://v8.dev/)

**Decision pending:** Will be evaluated based on MV/MZ compatibility requirements.

### PE Parser (for native DLL inspection)

**Candidates:**
- **libpe-parse** — MIT License
  - C++ PE format parser
  - [https://github.com/avast/libpe-parse](https://github.com/avast/libpe-parse)

- **pe-parse** — BSD License
  - C++ PE parser with Python bindings
  - [https://github.com/avast/pe-parse](https://github.com/avast/pe-parse)

**Decision pending:** Will be evaluated based on Win32 API compatibility requirements.

## Licensing Principles

1. **Prefer MIT/Apache 2.0/BSD** licensed libraries
2. **Avoid copyleft licenses** (GPL, AGPL) unless absolutely necessary
3. **Never bundle** proprietary RPG Maker code or RTP assets
4. **Document** all third-party components with license text
5. **Attribute** all third-party contributors

## Adding New Dependencies

Before adding any third-party dependency:

1. Evaluate license compatibility
2. Assess footprint impact
3. Consider self-implementation alternatives
4. Document in this file
5. Include license text in repository

## RPG Maker Engine & RTP

**Not included.** UniversalRPG implements RPG Maker behavior independently.

- No proprietary RPG Maker engine code
- No original runtime binaries
- No RTP assets
- Users must provide their own legally obtained games and RTP
