#Optimization mod by Teravisor

#Adds "Optimized!" label on adult content warning.

#unless Scene_AdultContentWarning.instance_methods(false).include?(:start_)
#
#class Scene_AdultContentWarning < Scene_Base
#	alias :start_ :start
#	alias :terminate_ :terminate
#	
#	def start
#		start_
#		@label = Sprite.new(@viewport)
#		@label.bitmap = Bitmap.new(128,32)
#		@label.x = 16
#		@label.y = 16
#		@label.z = 201
#		@label.bitmap.draw_text(0,0,200,32,"Optimized V21_1",0)
#	end
#	def terminate
#		terminate_
#		@label.bitmap.dispose
#		@label.dispose
#	end
#end
#
#module DataManager
#	class << self
#	alias_method :load_normal_database_OptimizationLabel, :load_normal_database
#	def load_normal_database
#		load_normal_database_OptimizationLabel
#		$data_system.game_title = $data_system.game_title+" O"
#	end
#
#	end
#end
#
#
#end
