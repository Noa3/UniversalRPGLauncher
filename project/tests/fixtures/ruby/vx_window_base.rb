# The first script of an RPG Maker VX project, the way RPG_RT writes it.
# A class that inherits one the reader has never heard of, a constant,
# two conditionals with an else, a top-level call, and a method that
# assigns to the reader.
class Window_Base < Window
  DEFAULT_X = 123

  def initialize(actor)
    @top = 123
    if @name == 0
      @lam = 0
    end
    if @cipher == @top
      @lam = 123
    else
      @lam = 0
    end
  end

  def refresh
    window.width = $lam
    window.height = $lam
    window.contents.clear
  end
end
