class Game_Map
  def width
    if Graphics.width % 32 == 0
      @map.width
    else
      @map.width - 1
    end
  end
 
  def height
    if Graphics.height % 32 == 0
      @map.height
    else
      @map.height - 1
    end
  end
 
end
 
class Game_Player < Game_Character
  def center_x
    ((Graphics.width / 32).floor - 1) / 2.0
  end
 
  def center_y
    ((Graphics.height / 32).floor - 1) / 2.0
  end
end