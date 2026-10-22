# Bounded RGSS sprite-composition reference

This is the next implementation contract under `K-PROD-RGSS-RENDERING`, not an
implemented or verified renderer. Initial scope: unscaled, unrotated, normal-blend
sprites without viewports. Other effects must remain explicitly unsupported.

## Reference and evidence

Implementation reference: mkxp-z v2.4.2, commit
`290ffe1135142d91a40b8f295a7b8e284f432c9a`. Parent inspection confirmed the source
branches below after independent reference research. This is compatibility
reference evidence, not execution against the proprietary RGSS engines.

- [Scene ordering](https://github.com/mkxp-z/mkxp-z/blob/290ffe1135142d91a40b8f295a7b8e284f432c9a/src/display/gl/scene.cpp#L166-L190)
- [Sprite bitmap assignment and y setter](https://github.com/mkxp-z/mkxp-z/blob/290ffe1135142d91a40b8f295a7b8e284f432c9a/src/display/sprite.cpp#L365-L409)
- [Transform matrix](https://github.com/mkxp-z/mkxp-z/blob/290ffe1135142d91a40b8f295a7b8e284f432c9a/src/display/gl/transform.h#L126-L143)
- [Blend factors](https://github.com/mkxp-z/mkxp-z/blob/290ffe1135142d91a40b8f295a7b8e284f432c9a/src/display/gl/glstate.cpp#L59-L80)

## Confirmed source behavior

**Draw order:** ascending z, then ascending raw sprite y only for RGSS2/3, then
creation order. Later drawing is in front. XP skips the y comparison. Use the raw
y property, not y minus origin or the bottom edge. Do not apply the RGSS2/3 y rule
to XP merely because it seems suitable for a map renderer. Creation order must
remain stable when a sprite changes position or z.

**Bitmap assignment:** assigning the same bitmap does nothing, preserving a
custom src_rect. A different live bitmap resets the existing source rectangle to
its full bounds. Assigning nil or an already disposed bitmap changes the bitmap
reference but leaves the rectangle unchanged; it does not raise at assignment.
Later changes to the rectangle object must be reflected in subsequent frames.
The reset behavior is supported by mkxp-z, not explicitly by the engine manuals.

**Placement:** with unit scale, zero rotation and no viewport offset, position is
(x - ox, y - oy). y increases downward. Source clipping must not silently change
this anchor. Out-of-bounds, inverted source rectangles remain a separate parity
gate: the reference itself documents limitations, so do not invent exact original
behavior for those cases.

**Normal blending:** source texture data is straight RGBA. Opacity scales source
alpha, not source RGB. The reference uses RGB factors SRC_ALPHA and
ONE_MINUS_SRC_ALPHA, with alpha factors ONE and ONE_MINUS_SRC_ALPHA. If the CPU
frame starts as opaque black and remains opaque, apply that convention directly;
if an intermediate transparent surface is introduced, specify its alpha
representation and conversion before feeding Godot, rather than accidentally
premultiplying twice. Verify byte rounding with explicit expected-pixel fixtures.
The existing Rm2kPixelBuffer.PaintOver is not this operation: it replaces every
nonzero-alpha source pixel.

## Remaining research claims to verify during implementation

The independent research also identified visible=false, zero opacity and
nil/disposed bitmap as draw skips, with opacity clamped to0..255. Sprite disposal
is idempotent but subsequent ordinary sprite-property access raises a named
RGSSError. Verify these specific branches and the Ruby binding before wiring
setters; the parent has not yet traced all of them. No original-runtime result
establishes the XP tie-break or bitmap-reset behavior independently of mkxp-z.

## Integration and acceptance gates

1. Register actual Ruby Sprite instances and unregister on dispose; do not infer
   live sprites from a few globals. Bound both registry size and per-frame work.
2. Read mutable sprite/bitmap state only on the interpreter owner. Compose before
   the existing frame rendezvous publishes AtFrame; never mutate a published
   frame when the worker resumes.
3. Through ordinary runtime Update, assert exact pixels for overlap, origin,
   source rectangle, movement, visibility, disposal and generation-specific order.
   A previously returned frame must stay unchanged after another Update.
4. Route the resulting frame into the real Godot screen on the main thread.
   Rm2kGameScreen.SetGameState already copies bytes and requests redraw; no Godot
   object may be touched by the worker.
5. Keep rendering capability and production claims bounded. Windows, tilemaps,
   viewports, file decoding, transforms, tone/color/bush/wave effects, transitions,
   input, pacing, audio and saves are not supplied by this initial compositor.
