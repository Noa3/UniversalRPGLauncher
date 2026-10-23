# Bounded RGSS sprite composition

This is a local implementation boundary under K-PROD-RGSS-RENDERING, not a claim
that XP, VX or VX Ace is playable or production-complete. The pinned reference
contract and its uncertainties are in `RGSS_SPRITE_REFERENCE.md`.

## Implemented path

The generation bootstraps install `RgssSpriteRubySource` after the original
classes. Sprite initialization registers its actual Ruby object in the worker's
`RgssSpriteStore`; disposal unregisters it. A different live bitmap resets the
existing source rectangle, while the same bitmap, nil and a disposed bitmap do
not. Rectangle mutation is read directly at the next composition boundary.

`Graphics.update` forwards the current width and height to `RgssGrafikHost`.
The host composes on the Ruby owner thread before entering the frame rendezvous.
XP sorts by z and creation order; VX/VX Ace also compare raw y between those keys.
Source rectangles and x-minus-ox/y-minus-oy positions are clipped with widened
arithmetic. Negative rectangle extents are empty, not normalized or inverted.

Normal composition uses straight source RGBA and the combined source-alpha /
opacity weight over an opaque black screen. The deterministic integer rounding
is tested; no pixel-perfect agreement with an original GPU implementation is
claimed. `Rm2kPixelBuffer.PaintOver` is deliberately not used for blending.

`RgssFrameSession` publishes `RgssRenderedFrame` under its synchronization gate.
The frame exposes no writable worker storage: `CopyPixels` returns a detached
buffer. Prior frames remain stable after later updates, consumer writes and
runtime disposal. Sprite/bitmap resources are released only after worker join.

## Resource and compatibility bounds

- At most 4096 registered sprites; identifiers are not recycled.
- At most 4 Mi output pixels and 16 Mi sampled sprite pixels per frame.
- Width and height must be positive and no greater than 4096 individually.
- The existing bitmap count and 64 MiB bitmap-storage cap remain independent.
- Composition checks cancellation before work and on each drawn row.
- Viewports, transforms, mirror, bush, non-normal blends, wave, color and tone
  are not implemented. Visible sprites requiring them are omitted with a frame
  warning; fractional effect values must not be truncated into apparent support.
- Sprite copying is explicitly refused rather than aliasing registry handles.
- This is not the complete Sprite API. General disposed-property guards,
  width/height forwarding, flash/update effects and complete error-class parity
  remain open. The inherited VX/VX Ace default screen sizes also need their own
  compatibility correction; a resize is honored by the compositor.
- File-based Bitmap pixels are still not decoded. Text, blits, windows, planes,
  tilemaps, transitions, input, audio, saves and frame pacing remain separate gates.

## Project startup ownership

Initialization prepares built-ins and the script source list, but does not run a
project's entry script synchronously. The complete ordered project script list
runs once on the same worker that preserves its stack across graphics updates.
No entry-script name is guessed. Project code returning means completion: the
host neither synthesizes new-game globals nor replays a leftover `$scene`.

The first frame has a 10-second startup watchdog; subsequent updates retain the
1-second scene watchdog. These bounds are not responsive asynchronous UI or
pacing. An immutable `ScriptProgress` receipt distinguishes pending, running,
completed, cancelled, frame-budget termination and faults, with attempted,
completed and failed counts. A script paused inside its main loop has not
completed merely because frames were produced. Named script failures are
reported and later scripts can run; cancellation and frame-budget termination
propagate without running the remaining list.

## Evidence and remaining acceptance

Focused `TestRgssSpriteFrames`: 16/16, covering the three bootstraps, ordinary
updates, exact pixels, alpha/opacity, ordering, clipping, bitmap/rectangle
identity, unsupported effects, resource/work limits, cancellation, detached
snapshots, disposal, entry-script ordering/locals and startup receipts/deadlines.
`TestRubyBlockBodyDispatch`: 2/2, including call-first method/branch/rescue/ensure
bodies. Resumable-scene tests: 13/13. Real XP execution: 2/2, which proves host
boundaries and lifecycle, not a visible or playable real game.

Initial canonical validation failed with22 cases in14 legacy RGSS suites reporting
nil/integer division during script loading. The frozen source inputs were stable;
the named reproduction traced it to Main running in partial library-test hosts.
Test-only library setup now parses all90 sources, executes89 library scripts and
defers that known entry to full-runtime tests. Those tests explicitly require Main
to stay executing across real updates with no script failures. All34 affected
cases pass focused checks. The corrected full run passed3051 tests:388 suite
receipts/3031 cases plus20 smoke checks, exit0, all692 frozen inputs unchanged.
Build148 warnings/0 errors; known native stderr and3 CanvasItem RID/6 ObjectDB
teardown warnings remain unchanged from the earlier bitmap baseline.

The subsequent Godot consumer is now implemented locally. Native texture tests
pass2/2, including exact-size data after shrinking and reset state. The actual
Main._Process route passes1/1 across XP/VX/VX Ace with native texture readback,
authoritative engine frame numbers, pause and stop. A standalone OpenGL witness
instantiates scenes/main.tscn and captures synthetic XP red/green frames from the
native viewport, exit0 without native diagnostics. Reproduce with
res://tests/acceptance/rgss_view_render_smoke.tscn and --output=<isolated directory>.
Full working-tree validation of this UI delta passed3054 tests (390 suites/3034
cases plus20 smoke checks), exit0;696 frozen inputs unchanged. Build148 warnings,
0 errors; existing3 CanvasItem RID/6 ObjectDB leaks remain. The3051 result above
describes the prior core checkpoint. Launcher gates and capabilities are unchanged. The test-installed
runtime is not evidence that an ordinary user can start an Experimental engine.
