# Third-Party Components and Licenses

> **Last reviewed:** 2026-09-11

This file records third-party components that are actually distributed with UniversalRPG and tracks prospective dependencies separately.

A candidate listed below is **not** an approved dependency until its version, license, platform support and integration strategy are reviewed.

## Currently Distributed

### Noto Sans CJK SC

- **Component:** `project/assets/fonts/NotoSansCJKsc-Regular.otf`
- **Purpose:** launcher Latin/Japanese/Korean/Simplified-Chinese glyph coverage
- **Upstream:** notofonts/noto-cjk
- **License:** SIL Open Font License 1.1
- **License text:** `project/assets/fonts/OFL.txt`

## Prospective Runtime Dependencies

No Ruby VM, JavaScript VM or PE/native execution library is currently vendored or selected as the final canonical dependency.

The public runtime contracts deliberately use `IEmbeddedScriptVm` / `IEmbeddedScriptVmFactory` so these implementation choices can change without breaking external SDK consumers.

See `docs/VM_EVALUATION.md`.

### Embedded Ruby / RGSS — preferred spike family: CRuby

Current architecture direction:

- RGSS1 -> `ruby-rgss1` / Ruby-1.8-compatible profile
- RGSS2 -> `ruby-rgss2` / Ruby-1.8-compatible profile
- RGSS3 -> `ruby-rgss3` / Ruby-1.9.2-compatible profile

Ruby exposes native embedding APIs suitable for defining host classes/methods and evaluating Ruby inside an application.

Ruby is distributed under the Ruby license with a 2-clause BSD option, but an actual vendored version still requires inspection of that release's `COPYING`/`BSDL`/`LEGAL` files and any bundled third-party components.

Before adding CRuby binaries/sources:

- decide whether historical builds or a newer compatibility implementation is used per RGSS generation
- prove Windows/Linux/Android build viability
- prove clean create/reset/dispose behavior
- implement resource/host-access policy around the VM
- test representative historical Ruby semantics used by custom RGSS scripts
- pin exact version/commit and all required notices

Do not describe a current CRuby build as RGSS1/2/3 compatible solely because it can execute Ruby syntax.

### Embedded JavaScript / MV/MZ — preferred first spike: QuickJS / QuickJS-ng

QuickJS is a small embeddable JavaScript engine. QuickJS-ng continues this design and is MIT-licensed.

It is currently the preferred **first implementation spike**, not an approved shipped dependency.

Reasons for the spike:

- small native embedding surface
- low startup overhead
- permissive MIT licensing
- VM can remain isolated behind `IEmbeddedScriptVm`
- practical fit for a custom browser/RPG Maker compatibility layer

Before adding it to distributed builds:

- pin exact upstream/version/commit
- verify original license text and transitive sources
- prove Windows/Linux/Android builds
- implement memory/interruption/watchdog limits
- disable OS/process/std helpers by default
- prove exception/stack conversion
- test MV/MZ plugin order through `WebScriptRuntime`
- build browser/RPG Maker APIs separately above the VM

A JavaScript VM alone does not provide Canvas, WebGL, WebAudio, DOM, storage, RPG Maker globals, or Node/NW.js compatibility.

### Node / NW.js Compatibility

Do not adopt unrestricted Node.js as the default MV/MZ host merely for plugin compatibility.

Preferred approach:

1. inventory API requirements
2. implement bounded shims (`path`, game-VFS `fs`, Buffer, limited process metadata)
3. keep process execution/network/native access policy-gated
4. treat `.node` addons as a separate native compatibility class

### PE / Native Binary Inspection

Initial native work must remain metadata-only.

A future library or self-implementation may be evaluated for:

- PE headers
- architecture
- imports/exports
- version metadata
- hashes

Selecting a PE parser does not authorize native DLL execution.

## Asset Licensing

If future development sources textures, audio, fonts, UI graphics or other game assets externally, record at minimum:

- exact asset
- original author/source
- source URL
- license name/version
- commercial-use permission
- modification permission
- redistribution permission
- attribution requirement
- local modifications

Prefer assets with clear redistribution terms. “Free download” without a clear license is not sufficient.

Do not use ripped assets, unofficial reposts or proprietary RPG Maker/WOLF/game assets without permission.

A dedicated asset manifest may be added when external game-facing assets begin to be included.

## Licensing Principles

1. Prefer permissive or clearly compatible licenses.
2. Review copyleft obligations before introducing them; do not reject a license category solely by label without understanding its actual obligations.
3. Never bundle proprietary RPG Maker/WOLF runtime binaries or RTP assets without explicit redistribution rights.
4. Include required license text and attribution with distributed third-party components.
5. Pin source/version information for reproducibility.
6. Re-evaluate licensing when a dependency changes distribution mode (for example static vs dynamic linking).
7. Keep planned candidates separate from components actually shipped.
8. Do not vendor a VM merely to make a prototype pass; platform/security/compatibility requirements apply before distribution.

## Adding a Dependency

Before merging a new third-party runtime dependency:

1. identify exact upstream project/version/commit
2. verify license from the original source
3. confirm Windows/Linux/Android implications
4. assess security/sandbox impact
5. assess binary size/performance impact
6. add regression/build coverage
7. update this file and include required license texts
8. ensure release packaging contains required notices
9. record how the dependency is replaced/mock-tested behind the public SDK boundary

## Proprietary Engine / RTP Data

Not distributed by UniversalRPG unless an explicit license permits it.

Users provide legally obtained games and required RTP resources.

The project aims to implement compatible behavior independently rather than redistributing original engine runtimes.
