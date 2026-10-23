# RGSS numeric Bitmap pixel-storage boundary

This is a bounded step under `K-PROD-RGSS-RENDERING`, not a complete Bitmap API
or a rendered game. Detection and parsing still execute no game scripts.

This document records the numeric-storage slice and its historical validation.
Later sprite/frame work is described in RGSS_SPRITE_BOUNDARY.md; filename/cache
PNG decoding now has a real pixel implementation described in RGSS_PNG_BOUNDARY.md.

## Implemented path

The three production bootstraps install `RgssBitmapRubySource` after their
value classes and before game scripts. Numeric `Bitmap.new(width, height)`
allocates a surface in the per-runtime `RgssGrafikHost.Bitmaps` store. The store
reuses the existing platform-independent `Rm2kPixelBuffer` RGBA byte layout.

`set_pixel`, `get_pixel`, `clear`, `dispose` and both `fill_rect` forms reach
actual storage. Mutators set_pixel/clear/fill_rect return their Ruby receiver.
`get_pixel` constructs a separate Color, not a reference into pixel memory.
`clear_rect` is installed only by VX and VX Ace, with numeric and Rect forms.
Clearing writes transparent black; filling replaces RGBA rather than blending.
`dup`/`clone` invoke `initialize_copy` on the new instance and copy pixel bytes
into an independently budgeted surface. Disposing either bitmap leaves the other
usable. The existing cache adapters reuse a bitmap for each kind/name and replace
disposed entries; clearing the cache explicitly disposes its retained surfaces.
Individual disposal is idempotent. Runtime disposal releases abandoned surfaces
only after its worker has joined; a failed join cannot release live storage.

## Resource and safety policy

Limits are implementation policy, not claims about original RGSS limits:

- Positive integer dimensions, each at most 4096.
- At most 4096 live surfaces and 64 MiB of combined live pixel arrays.
- Validate before allocation. These are live-data bounds, not a hard process-RSS
  limit or a complete CPU/memory sandbox for Ruby itself.
- Handles are per runtime and never recycled, including after ReleaseAll.
- Pixel writes outside the bitmap are no-ops; reads return transparent black.
- Normalize negative rectangle extents using 64-bit intermediate arithmetic,
  then clip to the bitmap before entering drawing loops.
- Clamp finite numeric color channels to 0..255 and truncate fractional bytes.
  Reject non-finite channels and coordinates outside the supported integer range.
  Finite float pixel coordinates and rectangle extents truncate toward zero;
  constructor dimensions currently still require integers.
- Pixel access after disposal fails with a named RGSSError. Filename construction
  uses the separately bounded PNG path; unsupported formats are refused instead
  of pretending to load image pixels.

## Evidence sources and limits

The generation-specific public API was checked in the mirrored engine manuals:

- [XP / RGSS1 Bitmap](https://www.rpg-maker.fr/dl/monos/aide/xp/source/rgss/gc_bitmap.html)
- [VX / RGSS2 Bitmap](https://www.rpg-maker.fr/dl/monos/aide/vx/source/rgss/gc_bitmap.html)
- [VX Ace / RGSS3 Bitmap](https://rpgmaker.fixato.org/Manual/RPGVXAce/rgss/gc_bitmap.html)
  (the host timed out during this work; the independent research used its
  [archived copy](http://web.archive.org/web/20250927144041/https://rpgmaker.fixato.org/Manual/RPGVXAce/rgss/gc_bitmap.html)).

The manuals specify the operations but do not settle invalid dimensions or
out-of-bounds behavior. Edge conventions use the independently inspected
[mkxp-z v2.4.2 bitmap implementation](https://github.com/mkxp-z/mkxp-z/blob/290ffe1135142d91a40b8f295a7b8e284f432c9a/src/display/bitmap.cpp)
and its [Ruby binding](https://github.com/mkxp-z/mkxp-z/blob/290ffe1135142d91a40b8f295a7b8e284f432c9a/binding/bitmap-binding.cpp):
negative-extent normalization, transparent out-of-bounds reads, independent
Color results, mutator return-self, and byte clamping. These are implementation
comparisons, not tests against the original proprietary runtime. In particular,
we deliberately do not copy the unchecked upstream out-of-bounds setPixel write.
Our bounded dimension/argument errors and coercion policy are not asserted to
match every original exception class or Ruby numeric conversion.

## Validation and unfinished work

`TestRgssBitmapPixels` exercises real bootstraps for channel storage, independent
bitmaps, clearing, idempotent disposal, both fill overloads, clipping, RGBA
replacement and generation-specific clear_rect. It also checks total allocation,
object-count limits, extreme coordinate arithmetic, non-recycled handles and
post-join runtime cleanup. Set-pixel and right-edge fill mutations were detected;
the original source was restored and rebuilt. After independent review, cache
growth, duplicate-handle aliasing and fractional-coordinate refusal were each
reproduced and fixed. Current pixel suite: 10/10; copy-hook suite: 2/2. The broader
Ruby interpreter suite passes 271/271 in a fresh process after removing CP932
provider-registration order dependencies from source decoding and four fixtures.
Font1/1, real XP2/2 and resumable scene13/13 also pass.

The pre-review full validator passed 3023 tests / 383 suite records, exit0.
The final corrected-source full validator passes 3030 tests: 3010 across 385 suite
records plus 20 runner smoke checks, exit0 and the final success marker.
The frozen code hashes match. The known 3 CanvasItem RID and 6 ObjectDB shutdown
warnings remain. Final-delta review had exposed exception-unwind context
retention and unvalidated image metadata. These were reproduced and corrected:
method state restores in finally, and PNG metadata checks the complete IHDR
header prefix and supported positive dimensions before returning sizes. This is
not CRC validation or full image decoding. Focused copy-hook2/2 and metadata1/1
pass; these final corrections are included in the3030-test full validation.

Known compatibility limits include invalid fill-overload/error-class parity,
constructor dimension coercion, and the interpreter's existing recursive-copy
behavior. This slice adds initialize_copy, not full initialize_dup/initialize_clone
dispatch or complete Ruby object-copy semantics. It does not claim that source
script names appear in every runtime exception.

File decoding, sprite composition and Godot frame publication are covered by
the later bounded slices linked above. RTP image resolution, blt/stretch_blt,
text/font drawing, tilemaps and input remain separate unfinished gates.
Some older bootstrap methods outside this slice are still stubs; their existence
is not a support claim. No Rendering capability is enabled by offscreen storage.
