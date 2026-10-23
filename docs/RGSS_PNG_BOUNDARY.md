# Bounded RGSS PNG loading

## Implemented boundary

`Bitmap.new(filename)` loads game-relative PNG pixels through the managed
`RgssBitmapFileLoader` and `RgssPngImage`. `RPG::Cache` now uses this path instead
of creating an empty numeric bitmap from header dimensions. Repeated lookups
reuse the same object; disposal/replacement and cache clear retain their existing
ownership behavior. No imported code executes during detection or parsing.

Supported PNG samples are non-interlaced RGB/RGBA at 8 bits per channel and
indexed images at 1/2/4/8 bits per pixel. Palette and RGB tRNS transparency are
preserved; palette index zero is not automatically transparent. All five PNG
scanline filters and consecutive IDAT chunks are handled. Grayscale, 16-bit
channels and interlacing are named refusals, not claimed compatibility.

The loader validates signature, chunk bounds/order, CRCs, critical chunk support,
zlib header/trailer, declared row length and palette indices. It decodes one row
at a time into the reserved bitmap surface and rejects extra inflated image data.
Failures after allocation release the surface. Cancellation is propagated rather
than converted to an image success or missing-file result.

Limits are explicit: 32 MiB encoded file, 4096 chunks, 4096 pixels per dimension,
and the existing 4096-surface/64 MiB aggregate RGBA store budget. Encoded buffers,
compressed working storage and rows are additional bounded temporary memory;
64 MiB is not a total-process-memory claim. Large otherwise valid files can be
refused by these limits. No new dependency or Godot call was added to the worker.

Paths are relative to the configured game root, with optional .png suffix,
backslash normalization and bounded case-insensitive directory lookup. Traversal,
absolute/drive/stream paths, NULs, ambiguous case fallback and reparse-point
components are refused. Lookup limits are 128 components and 32768 inspected
entries per component. Static link rejection is not an OS sandbox: concurrent
filesystem replacement and blocking filesystem I/O are not eliminated by this
managed path walk. Root ancestors are checked too, which can refuse linked game
installations. RTP mounts, archive images, JPEG/BMP and full engine-specific
filename/encoding semantics remain outside this slice.

Missing/unsafe cache lookups return nil with a named diagnostic. Malformed files
that exist raise a named Ruby error on load instead of producing empty artwork.
Direct missing-file construction reports Errno::ENOENT; decode errors report
RGSSError. Resource limits retain their existing RuntimeError refusal.

## Evidence and status

Focused TestRgssBitmapFiles passes7/7, covering all three production bootstraps,
file-backed cache-to-frame composition, native PNG decoder comparisons for RGB/
RGBA filters and indexed depths, alpha, malformed input, paths, size/memory
bounds and cancellation. Existing bitmap10/10, metadata1/1, font1/1, sprite16/16
and real-XP2/2 checks pass. The cache ownership test now uses actual PNG fixtures
instead of overriding the old metadata-only helper; its ownership assertions
are unchanged.

A separate OpenGL run of the actual launcher scene captured a synthetic cached
PNG as red, then a bitmap mutation as green at1100x700, and verified stop clears
the view. Exit0 with no native diagnostics; the red capture was inspected.
This is an actual file-backed rendering witness, not proof of a complete imported
game. Reproduce with the pinned console engine and
`res://tests/acceptance/rgss_view_render_smoke.tscn -- --output=<isolated directory>`.
Do not use headless mode for the viewport capture.

Canonical validation passes3061 tests:391 suites/3041 cases plus20 smoke checks,
exit0, all700 frozen inputs unchanged. Build148 warnings and known native error/
shutdown diagnostics exactly match the preceding canonical run; this is not
a clean-stderr or leak-free result. Completed final-delta review records no new
PNG-loader blocker; independent existing data-loader issues remain tracked.
This bounded slice is verified, not a complete RGSS implementation. Launch gates and Rendering
capability declarations remain unchanged. Blit/text/window/tilemap operations,
input, audio, pacing and full XP/VX/VX Ace compatibility remain incomplete.

The legacy BildGroesse metadata host entry is NOT covered by the constructor/
cache resolver. Full review found its raw Path.Combine/File.OpenRead route still
permits rooted/traversal access; source inspection confirmed this separate
pre-existing issue. K-PROD-RGSS-IMAGE-METADATA-PATH tracks its closure. The
successful PNG tests must not be read as confinement of every image-host entry.
