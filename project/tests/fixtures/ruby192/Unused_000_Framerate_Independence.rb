#===============================================================================
# Framerate Independence
# By Jet10985 (Jet)
#===============================================================================
# This snippet will change the inner movement and time-based operations of
# Ace to accomadate a lower fps than expected, meaning it will experience the
# same change despite not having been interated over as many times as expected.
# This script has: 0 customization options.
#===============================================================================
# Overwritten Methods:
# Spriteset_Weather: update_sprite_rain, update_sprite_snow, update_sprite_storm
# Game_Map: update_parallax
# Game_Picture: update_rotate
# Game_Screen: update_weather
#-------------------------------------------------------------------------------
# Aliased methods:
# Graphics: update
# Game_CharacterBase: distance_per_frame
# Game_Map: scroll_distance
# Game_Picture: update_move, update_tone_change
# Game_Screen: update_fadeout, update_fadein, update_shake, update_flash,
#   update_tone
#===============================================================================
 
class << Graphics
 
  def delta
    @delta
  end
 
  def update_delta
    @this_frame = Time.now
    @delta = (@this_frame - @last_frame)
    @last_frame = @this_frame
    if @delta >= 1 || @delta == 0.0
      @delta = 1 / Graphics.frame_rate.to_f
    end
  end
 
  def fps_difference
    ((1 / Graphics.delta).round / Graphics.frame_rate.to_f)
  end
 
  alias jet2734_update update
  def update(*args, &block)
    jet2734_update(*args, &block)
    @last_frame ||= Time.now
    update_delta
  end
end
 
class Game_CharacterBase
 
  alias jet3845_distance_per_frame distance_per_frame
  def distance_per_frame(*args, &block)
    jet3845_distance_per_frame(*args, &block) / Graphics.fps_difference
  end
end
 
class Spriteset_Weather
 
  def update_sprite_rain(sprite)
    sprite.bitmap = @rain_bitmap
    sprite.x -= Graphics.frame_rate * Graphics.delta
    sprite.y += Graphics.frame_rate * Graphics.delta * 6
    sprite.opacity -= Graphics.frame_rate * Graphics.delta * 12
  end
 
  def update_sprite_storm(sprite)
    sprite.bitmap = @storm_bitmap
    sprite.x -= Graphics.frame_rate * Graphics.delta * 3
    sprite.y += Graphics.frame_rate * Graphics.delta * 6
    sprite.opacity -= Graphics.frame_rate * Graphics.delta * 12
  end
 
  def update_sprite_snow(sprite)
    sprite.bitmap = @snow_bitmap
    sprite.x -= Graphics.frame_rate * Graphics.delta
    sprite.y += Graphics.frame_rate * Graphics.delta * 3
    sprite.opacity -= Graphics.frame_rate * Graphics.delta * 12
  end
end
 
class Game_Map
 
  alias jet7345_scroll_distance scroll_distance
  def scroll_distance(*args, &block)
    jet7345_scroll_distance(*args, &block) / Graphics.fps_difference
  end
 
  def update_parallax
    sx = @parallax_sx / 64.0
    sx /= Graphics.fps_difference
    sy = @parallax_sy / 64.0
    sy /= Graphics.fps_difference
    @parallax_x += sx if @parallax_loop_x
    @parallax_y += sy if @parallax_loop_y
  end
end
 
class Game_Picture
 
  alias jet2938_update_move update_move
  def update_move(*args, &block)
    did = @duration != 0
    jet2938_update_move(*args, &block)
    return if !did || @duration == 0
    @duration += 1
    @duration -= Graphics.frame_rate * Graphics.delta
    if @duration > 0 && @duration < 1
      @duration = 1
    else
      @duration = [@duration, 0].max
    end
  end
 
  alias jet2938_update_tone_change update_tone_change
  def update_tone_change(*args, &block)
    did = @tone_duration != 0
    jet2938_update_tone_change(*args, &block)
    return if !did || @tone_duration == 0
    @tone_duration += 1
    @tone_duration -= Graphics.frame_rate * Graphics.delta
    if @tone_duration > 0 && @tone_duration < 1
      @tone_duration = 1
    else
      @tone_duration = [@tone_duration, 0].max
    end
  end
 
  def update_rotate
    return if @rotate_speed == 0
    to_add = @rotate_speed / 2.0
    to_add /= Graphics.fps_difference
    @angle += to_add
    @angle += 360 while @angle < 0
    @angle %= 360
  end
end
 
class Game_Screen
 
  alias jet2938_update_fadeout update_fadeout
  def update_fadeout(*args, &block)
    did = @fadeout_duration != 0
    jet2938_update_fadeout(*args, &block)
    return if !did || @fadeout_duration == 0
    @fadeout_duration += 1
    @fadeout_duration -= Graphics.frame_rate * Graphics.delta
    if @fadeout_duration > 0 && @fadeout_duration < 1
      @fadeout_duration = 1
    else
      @fadeout_duration = [@fadeout_duration, 0].max
    end
  end
 
  alias jet2938_update_fadein update_fadein
  def update_fadein(*args, &block)
    did = @fadein_duration != 0
    jet2938_update_fadein(*args, &block)
    return if !did || @fadein_duration == 0
    @fadein_duration += 1
    @fadein_duration -= Graphics.frame_rate * Graphics.delta
    if @fadein_duration > 0 && @fadein_duration < 1
      @fadein_duration = 1
    else
      @fadein_duration = [@fadein_duration, 0].max
    end
  end
 
  alias jet2938_update_tone update_tone
  def update_tone(*args, &block)
    did = @tone_duration != 0
    jet2938_update_tone(*args, &block)
    return if !did || @tone_duration == 0
    @tone_duration += 1
    @tone_duration -= Graphics.frame_rate * Graphics.delta
    if @tone_duration > 0 && @tone_duration < 1
      @tone_duration = 1
    else
      @tone_duration = [@tone_duration, 0].max
    end
  end
 
 #bugged
  alias jet2938_update_flash update_flash
  def update_flash(*args, &block)
    did = @flash_duration != 0
    jet2938_update_flash(*args, &block)
    return if !did || @flash_duration == 0
    @flash_duration += 1
    @flash_duration -= Graphics.frame_rate * Graphics.delta
    if @flash_duration > 0 && @flash_duration < 1
      @flash_duration = 1
    else
      @flash_duration = [@flash_duration, 0].max
    end
  end
 
  alias jet2938_update_shake update_shake
  def update_shake(*args, &block)
    did = @shake_duration != 0
    jet2938_update_shake(*args, &block)
    return if !did || @shake_duration == 0
    @shake_duration += 1
    @shake_duration -= Graphics.frame_rate * Graphics.delta
    if @shake_duration > 0 && @shake_duration < 1
      @shake_duration = 1
    else
      @shake_duration = [@shake_duration, 0].max
    end
  end
 
  def update_weather
    if @weather_duration > 0
      d = @weather_duration
      @weather_power = (@weather_power * (d - 1) + @weather_power_target) / d
      @weather_duration -= Graphics.frame_rate * Graphics.delta
      if @weather_duration <= 0 && @weather_power_target == 0
        @weather_type = :none
      end
    end
  end
end