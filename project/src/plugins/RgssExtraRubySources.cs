using System;
using System.Collections.Generic;

namespace UniversalRPG.Plugins;

/// <summary>
/// Ruby sources for the engine modules a game calls every frame and that the
/// built-in list did not carry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these are separate sources and not one long list inside the
/// plugin, and that is a measured decision.</strong> The built-in list writes
/// its Ruby as <c>"module Graphics\n"</c> with a C# escape per line, and two
/// attempts to add to it produced real newlines inside the string literals --
/// which is a compile error, and which is why the layer did not ship. **A raw
/// string literal has no escapes at all**, so the Ruby below is the Ruby the
/// reader gets.
/// </para>
/// <para>
/// <strong>And each entry opens a module that may already exist.</strong> A
/// game's own script does exactly this when it adds a method to an engine
/// module, and a reader that could not reopen a module would have refused half
/// of every plugin written for this engine.
/// </para>
/// </remarks>
public static class RgssExtraRubySources
{
    /// <summary>
    /// The sources, in the order they are defined.
    /// </summary>
    public static readonly IReadOnlyList<string> Sources = new[]
    {
        // **And `Graphics.update` is the frame, and it was the one name the
        // engine's own loop asked for and did not get.** Measured after the
        // parser fix: the scene runs far enough to ask for `Graphics.update`
        // and `Graphics.frame_reset`, and a reader that answered neither left
        // `frame_count` at 0 for ever.
        //
        // **And the counter is the honest part.** There is no surface yet, so
        // nothing is presented -- and `frame_count` is what a game's own timer
        // and its animation counters read, so it has to advance or every one of
        // them stands still.
        """
        module Graphics
          def self.update
            $graphics_frames = $graphics_frames + 1
            rgss_present_frame()
          end

          def self.wait(duration)
            duration.times do
              Graphics.update
            end
          end

          # Only timing is reset; the headless host has no wall-clock scheduler.
          def self.frame_reset
          end
        end
        """,

        // **And `Audio` is a module a game calls on every scene change.**
        // Measured: the scene asked for `bgm_play`, `bgs_stop` and `me_stop`
        // and reported *"nil has no method"* for each -- because the module did
        // not exist and the receiver was nil.
        //
        // **And there is no audio device in a headless run, and that is the
        // honest state:** the calls keep the game's own state -- what is
        // playing, at what volume -- so a game that reads back what it set gets
        // its own answer, **and a game that expects to hear something gets
        // silence rather than a crash.**
        """
        module Audio
          $audio_bgm = nil
          $audio_bgs = nil
          $audio_me = nil
          $audio_se_count = 0
          $audio_bgm_volume = 100

          def self.bgm_play(name, volume = 100, pitch = 100)
            $audio_bgm = name
            $audio_bgm_volume = volume
          end

          def self.bgm_stop
            $audio_bgm = nil
          end

          def self.bgm_fade(t)
            $audio_bgm = nil
          end

          def self.bgm_pos
            0
          end

          def self.bgs_play(name, volume = 100, pitch = 100)
            $audio_bgs = name
          end

          def self.bgs_stop
            $audio_bgs = nil
          end

          def self.bgs_fade(t)
            $audio_bgs = nil
          end

          def self.bgs_pos
            0
          end

          def self.me_play(name, volume = 100, pitch = 100)
            $audio_me = name
          end

          def self.me_stop
            $audio_me = nil
          end

          def self.me_fade(t)
            $audio_me = nil
          end

          def self.se_play(name, volume = 100, pitch = 100)
            $audio_se_count = $audio_se_count + 1
          end

          def self.se_stop
          end
        end
        """,

        // **And `Rect` is what a cursor and a source rectangle are, and a game
        // calls `set` on them every frame.** Measured: the engine's own
        // `update_cursor_rect` ends in `self.cursor_rect.set(x, y, w, 32)`, and
        // the reader reported *"nil has no method 'set'"* -- because the
        // window's `cursor_rect` started as nil.
        //
        // **And `set` and `empty` return self**, which is Ruby's own answer and
        // the one a game chains from.
        """
        class Rect
          attr_accessor :x
          attr_accessor :y
          attr_accessor :width
          attr_accessor :height

          def initialize(x = 0, y = 0, width = 0, height = 0)
            @x = x
            @y = y
            @width = width
            @height = height
          end

          def set(x, y, width, height)
            @x = x
            @y = y
            @width = width
            @height = height
            self
          end

          def empty
            @x = 0
            @y = 0
            @width = 0
            @height = 0
            self
          end
        end
        """,

        // **And `Tone` and `Color` are the two value objects a window and a
        // sprite are given.** Measured: the scene asked for `color=` on a nil,
        // which is a game writing `self.color = Color.new(...)` -- and a reader
        // that had no such class answered nil for the class itself.
        """
        class Tone
          attr_accessor :red
          attr_accessor :green
          attr_accessor :blue
          attr_accessor :gray

          def initialize(red = 0, green = 0, blue = 0, gray = 0)
            @red = red
            @green = green
            @blue = blue
            @gray = gray
          end

          def set(red, green, blue, gray = 0)
            @red = red
            @green = green
            @blue = blue
            @gray = gray
            self
          end
        end
        """,

        // **And `Font` is what a Bitmap's text is drawn with.** Measured: the
        // engine's own `draw_item` does `self.contents.font.color = ...`, and the
        // reader reported *"nil has no method 'color='"* -- because a Bitmap's
        // `font` started as nil and the color had nothing to be set on.
        //
        // **And a font is state and not a drawing**, like everything else here:
        // a game sets the size, the colour and the boldness, and reads them back.
        """
        class Font
          attr_accessor :name
          attr_accessor :size
          attr_accessor :bold
          attr_accessor :italic
          attr_accessor :color

          def initialize(name = "Arial", size = 22)
            @name = name
            @size = size
            @bold = false
            @italic = false
            @color = Color.new(255, 255, 255, 255)
          end
        end
        """,

        """
        class Color
          attr_accessor :red
          attr_accessor :green
          attr_accessor :blue
          attr_accessor :alpha

          def initialize(red = 0, green = 0, blue = 0, alpha = 255)
            @red = red
            @green = green
            @blue = blue
            @alpha = alpha
          end

          def set(red, green, blue, alpha = 255)
            @red = red
            @green = green
            @blue = blue
            @alpha = alpha
            self
          end
        end
        """,
    };
}
