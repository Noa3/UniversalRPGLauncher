#Optimization mod by Teravisor

#Fixes a HUGE performance leak caused by all CGs being assembled at load and kept as viewport+sprite during whole gameplay.

#unless $portraits_optimized
#$portraits_optimized=true

#Rewrite of portrait system that makes it not store 200+ sprites and viewports.
class Portrait_System
	def update_auto_hide(portrait)
		return if $game_message.busy? || $game_map.interpreter.running?
		@portrait_idle[portrait]=0 if @portrait_idle[portrait].nil?
		@portrait_idle[portrait]+=1
		if(getSec(@portrait_idle[portrait])>=System_Settings::PORTRAIT_AUTO_HIDE_SEC)
			portrait.hide
			portraitDownStage(portrait)
			@portrait_idle.delete(portrait)
			portrait.delete_sprite
		end
	end
	def getPortrait(name)
		p "Missing Portrait #{name}, using character:\"nil\" instead" if @portraits[name].nil? if $degug_portrait
		return @portraits["nil"] if@portraits[name].nil?
		return @portraits[name]
	end
end

class Portrait
	def initialize(base_char,parts,canvas=[300,400,0,0])
		@portrait = FakeSprite.new
		@base_char=base_char
		@canvas=Array.new(canvas.length){|index|canvas[index].to_i}
		@prts=sort_parts(parts)
		#don't create portrait on init
	end

	def update
		#don't reassemble portrait if it's not visible
		return if !@portrait.visible
		update_parts
		assemble_portrait
	end

	def force_update
		update_parts
		assemble_portrait
	end


	def show
		if @portrait.sprite.nil? then
			create_portrait
		end
		@portrait.visible=true
		update
	end
  
	def unshow
		@portrait.visible=false
		delete_sprite
	end

	def create_portrait()
		create_sprite
		reset_position
		force_update
	end
	
	def create_sprite
		return if !@portrait.sprite.nil?
		@prt_vp=Viewport.new
		@prt_vp.z=System_Settings::PORTRAIT_MAP_Z
		@portrait.viewport = @prt_vp
		@portrait.show
		@portrait.visible=false
	end

	def delete_sprite
		return if @portrait.sprite.nil?
		@prt_vp.dispose
		@portrait.bitmap.dispose
		@portrait.dispose
		@portrait.viewport = nil
	end
end



class Lona_Portrait < Moveable_Portrait
	def delete_sprite
	end
	
#only call 102_Lona_Portrait_Rule.rb if viewport is set up
	alias_method :updateExtra_optimization, :updateExtra
	def updateExtra
		return if @prt_vp.nil?
		updateExtra_optimization
	end
end

class FakeSprite
	attr_accessor :x
	attr_accessor :y
	attr_accessor :z
	attr_accessor :bitmap
	attr_accessor :src_rect
	attr_accessor :viewport
	attr_accessor :visible
	attr_accessor :ox
	attr_accessor :oy
	attr_accessor :zoom_x
	attr_accessor :zoom_y
	attr_accessor :angle
	attr_accessor :wave_amp
	attr_accessor :wave_length
	attr_accessor :wave_speed
	attr_accessor :wave_phase
	attr_accessor :mirror
	attr_accessor :bush_depth
	attr_accessor :bush_opacity
	attr_accessor :opacity
	attr_accessor :blend_type
	attr_accessor :color
	attr_accessor :tone
	attr_accessor :sprite

	def initialize(viewport=nil)
		if viewport.class == Sprite then
			load_from_sprite(viewport)
		else
			@viewport = viewport
			@color = ColorHook.new
			@src_rect = Rect.new
			@visible=false
			@x=0
			@y=0
			@z=0
			@ox=0
			@oy=0
			@angle=0
			@mirror=false
			@opacity=255
			@blend_type=0
			@zoom_x = 1
			@zoom_y = 1
			@tone=ToneHook.new
			@color.update_method = method(:update_color)
			@tone.update_method = method(:update_tone)
		end
	end

	def load_from_sprite(sprite)
		@x = sprite.x
		@y = sprite.y
		@z = sprite.z
		@bitmap = sprite.bitmap
		@src_rect = sprite.src_rect
		@viewport = sprite.viewport
		@visible = sprite.visible
		@ox = sprite.ox
		@oy = sprite.oy
		@zoom_x = sprite.zoom_x
		@zoom_y = sprite.zoom_y
		@angle = sprite.angle
		@wave_amp = sprite.wave_amp
		@wave_length = sprite.wave_length
		@wave_speed = sprite.wave_speed
		@wave_phase = sprite.wave_phase
		@mirror = sprite.mirror
		@bush_depth = sprite.bush_depth
		@bush_opacity = sprite.bush_opacity
		@opacity = sprite.opacity
		@blend_type = sprite.blend_type
		@color = sprite.color
		@tone = sprite.tone
		@sprite = sprite
		hide if !@visible
	end
	
	def dispose
		@sprite.dispose if @sprite
		@sprite = nil
	end
	def disposed?
		return @sprite.nil? || @sprite.disposed?
	end
	def flash(color,duration)
		@sprite.flash(color,duration) if @sprite
	end
	def update
		@sprite.update if @sprite
	end
	def width
		return @src_rect.width if @src_rect
		return @sprite.width if @sprite
		return 0
	end
	def height
		return @src_rect.height if @src_rect
		return @sprite.height if @sprite
		return 0
	end

	def hide
		dispose
		@visible=false
	end

	def show
		@sprite = Sprite.new(viewport)
		@sprite.x = @x if @x
		@sprite.y = @y if @y
		@sprite.z = @z if @z
		@sprite.bitmap = @bitmap if @bitmap
		@sprite.src_rect = @src_rect if @src_rect
		@sprite.visible = @visible if @visible
		@sprite.ox = @ox if @ox
		@sprite.oy = @oy if @oy
		@sprite.zoom_x = @zoom_x if @zoom_x
		@sprite.zoom_y = @zoom_y if @zoom_y
		@sprite.angle = @angle if angle
		@sprite.wave_amp = @wave_amp if @wave_amp
		@sprite.wave_length = @wave_length if @wave_length
		@sprite.wave_speed = @wave_speed if @wave_speed
		@sprite.wave_phase = @wave_phase if @wave_phase
		@sprite.mirror = @mirror if @mirror
		@sprite.bush_depth = @bush_depth if @bush_depth
		@sprite.bush_opacity = @bush_opacity if @bush_opacity
		@sprite.opacity = @opacity if @opacity
		@sprite.blend_type = @blend_type if @blend_type
		@sprite.color = @color if @color
		@sprite.tone = @tone if @tone
	end
	
	def x=(val)
		@x = val
		@sprite.x = val if @sprite
	end
	def y=(val)
		@y = val
		@sprite.y = val if @sprite
	end
	def z=(val)
		@z = val
		@sprite.z = val if @sprite
	end
	def bitmap=(val)
		@bitmap = val
		@src_rect = @bitmap.rect
		@sprite.bitmap = val if @sprite
	end
	def src_rect=(val)
		@src_rect = val
		@sprite.src_rect = val if @sprite
	end
	def visible=(val)
		show if val && !@sprite
		hide if !val && @sprite
		@visible = val
		@sprite.visible = val if @sprite
	end
	def visible
		return @visible && @sprite
	end
	def ox=(val)
		@ox = val
		@sprite.ox=val if @sprite
	end
	def oy=(val)
		@oy = val
		@sprite.oy=val if @sprite
	end
	def zoom_x=(val)
		@zoom_x=val
		@sprite.zoom_x=val if @sprite
	end
	def zoom_y=(val)
		@zoom_y=val
		@sprite.zoom_y=val if @sprite
	end
	def angle=(val)
		@angle = val
		@sprite.angle = val if @sprite
	end
	def wave_amp=(val)
		@wave_amp=val
		@sprite.wave_amp=val if @sprite
	end
	def wave_length=(val)
		@wave_length=val
		@sprite.wave_length=val if @sprite
	end
	def wave_speed=(val)
		@wave_speed=val
		@sprite.wave_speed=val if @sprite
	end
	def wave_phase=(val)
		@wave_phase=val
		@sprite.wave_speed=val if @sprite
	end
	def mirror=(val)
		@mirror = val
		@sprite.mirror=val if @sprite
	end
	def bush_depth=(val)
		@bush_depth=val
		@sprite.bush_depth=val if @sprite
	end
	def bush_opacity=(val)
		@bush_opacity=val
		@sprite.bush_opacity=val if @sprite
	end
	def opacity=(val)
		@opacity = val
		@sprite.opacity = val if @sprite
	end
	def blend_type=(val)
		@blend_type=val
		@sprite.blend_type = val if @sprite
	end
	def color=(val)
		@color=ColorHook.new(val.red,val.green,val.blue,val.alpha)
		@color.update_method = method(update_color)
		@sprite.color = val if @sprite
	end
	def update_color(newcolor)
		@sprite.color = newcolor if @sprite
	end
	def tone=(val)
		@tone = ToneHook.new(val.red,val.green,val.blue,val.grey)
		@tone.update_method = method(update_tone)
		@sprite.color = val if @sprite
	end
	def update_tone(newtone)
		@sprite.tone = newtone if @sprite
	end
end

class ColorHook < Color
	attr_accessor :update_method

	def initialize(r=0,g=0,b=0,a=0)
		super(r,g,b,a)
	end
	def red=(val)
		super(val)
		update_method.call(self)
	end
	def blue=(val)
		super(val)
		update_method.call(self)
	end
	def green=(val)
		super(val)
		update_method.call(self)
	end
	def alpha=(val)
		super(val)
		update_method.call(self)
	end
end
class ToneHook < Tone
	attr_accessor :update_method
	def initialize(r=0,g=0,b=0,grey=0)
		super(r,g,b,grey)
	end
	def red=(val)
		super(val)
		update_method.call(self)
	end
	def blue=(val)
		super(val)
		update_method.call(self)
	end
	def green=(val)
		super(val)
		update_method.call(self)
	end
	def grey=(val)
		super(val)
		update_method.call(self)
	end
end

#end
