namespace UniversalRPG.Plugins;

/// <summary>Install after value-class bootstraps, before any game scripts.</summary>
internal static class RgssBitmapRubySource
{
    // Shared by the three existing cache adapters; cached images own decoded pixels.
    internal const string CacheSource = """
        module RPG::Cache
          def self.Bild(kind, name)
            @cache = {} if @cache == nil
            bucket = @cache[kind]
            if bucket == nil
              bucket = {}
              @cache[kind] = bucket
            end
            bitmap = bucket[name]
            if bitmap != nil
              return bitmap if !bitmap.disposed?
            end
            path = rgss_bitmap(:cache_path, kind, name)
            return nil if path == nil
            bitmap = Bitmap.new(path)
            bucket[name] = bitmap
            bitmap
          end

          def self.clear
            if @cache != nil
              @cache.values.each do |bucket|
                bucket.values.each do |bitmap|
                  bitmap.dispose
                end
              end
            end
            @cache = {}
            nil
          end
        end
        """;

    // RGSS2/3 only: the original XP API does not include clear_rect.
    internal const string ClearRectSource = """
        class Bitmap
          def clear_rect(x, y = nil, width = nil, height = nil)
            if y == nil
              fill_rect(x, Color.new(0, 0, 0, 0))
            else
              fill_rect(x, y, width, height, Color.new(0, 0, 0, 0))
            end
            self
          end
        end
        """;

    internal const string Source = """
        class Bitmap
          def initialize(width, height = nil)
            if height == nil
              loaded = rgss_bitmap(:load, width)
              @pixel_surface = loaded[0]
              @width = loaded[1]
              @height = loaded[2]
            else
              @pixel_surface = rgss_bitmap(:create, width, height)
              @width = width
              @height = height
            end
            @font = Font.new
            @disposed = false
          end

          def initialize_copy(original)
            @pixel_surface = rgss_bitmap(:copy, original.instance_variable_get(:@pixel_surface))
            self
          end

          def clear
            rgss_bitmap(:clear, @pixel_surface)
            self
          end

          def set_pixel(x, y, color)
            rgss_bitmap(:set_pixel, @pixel_surface, x, y, color.red, color.green, color.blue, color.alpha)
            self
          end

          def fill_rect(x, y, width = nil, height = nil, color = nil)
            if color == nil
              color = y
              rgss_bitmap(:fill_rect, @pixel_surface, x.x, x.y, x.width, x.height, color.red, color.green, color.blue, color.alpha)
            else
              rgss_bitmap(:fill_rect, @pixel_surface, x, y, width, height, color.red, color.green, color.blue, color.alpha)
            end
            self
          end

          def get_pixel(x, y)
            channels = rgss_bitmap(:get_pixel, @pixel_surface, x, y)
            Color.new(channels[0], channels[1], channels[2], channels[3])
          end

          def dispose
            rgss_bitmap(:dispose, @pixel_surface)
            @disposed = true
          end
        end
        """;
}
