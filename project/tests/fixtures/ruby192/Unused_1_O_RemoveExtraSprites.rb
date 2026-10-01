#Optimization mod by Teravisor

#MOG_Timer is being always kept as Sprite and possibly getting RPGMaker's update calls. Remove them when they're not used.

if !$sprites_optimized && $portraits_optimized
$sprites_optimized=true



#MOG_Timer keeps 4 Sprites up always even though it's shown only in like 3-4 minigames. Replacing them with FakeSprite (requires 0_O_PortraitOptimization.rb) that don't use resources when idle.
class Sprite_Timer < Sprite
	alias_method :initialize_Optimization, :initialize
	def initialize(viewport)
		initialize_Optimization(viewport)

		@layout = FakeSprite.new(@layout)
		@meter_sprite = FakeSprite.new(@meter_sprite)
		@number_sprite = FakeSprite.new(@number_sprite)
		@fire_sprite = FakeSprite.new(@fire_sprite)
	end
end




end