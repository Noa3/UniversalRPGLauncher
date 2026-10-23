namespace UniversalRPG.Plugins;

internal static class RgssSpriteRubySource
{
    public const string Source = """
        class Sprite
          def initialize(viewport = nil)
            @viewport = viewport
            @bitmap = nil
            @src_rect = Rect.new
            @x = 0; @y = 0; @z = 0
            @ox = 0; @oy = 0
            @opacity = 255; @visible = true; @disposed = false
            @angle = 0; @zoom_x = 1.0; @zoom_y = 1.0
            @mirror = false; @bush_depth = 0; @blend_type = 0
            @color = Color.new(0, 0, 0, 0); @tone = Tone.new
            @sprite_handle = rgss_sprite(:create, self)
          end
          def bitmap=(value)
            if rgss_sprite(:assign, @sprite_handle, value)
              @src_rect.set(0, 0, value.width, value.height)
            end
            value
          end
          def dispose
            rgss_sprite(:dispose, @sprite_handle)
            @disposed = true
          end
          def disposed?; @disposed; end
          def opacity=(value)
            rgss_sprite(:opacity, @sprite_handle, value)
          end
          def initialize_copy(original)
            rgss_sprite(:copy, @sprite_handle)
          end
        end
        """;
}
