# frozen_string_literal: true

module SceneManager
  def self.call(scene_class)
    @stack.push(@scene)
    @scene = scene_class.new
  end

  def self.return
    @scene = @stack.pop
  end
end