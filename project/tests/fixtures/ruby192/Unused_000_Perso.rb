class Window_Command < Window_Selectable

  #--------------------------------------------------------------------------
  # * Get Window Width
  #--------------------------------------------------------------------------
  def window_width
    return 200
  end
end
  
  
  
class Window_MenuCommand < Window_Command
   
  #--------------------------------------------------------------------------
  # * Get Window Width
  #--------------------------------------------------------------------------
  def window_width
    return 200
  end
end




class Window_MenuStatus < Window_Selectable

  #--------------------------------------------------------------------------
  # * Get Window Width
  #--------------------------------------------------------------------------
  def window_width
    Graphics.width - 200
  end
end




class Window_SkillCommand < Window_Command

  #--------------------------------------------------------------------------
  # * Get Window Width
  #--------------------------------------------------------------------------
  def window_width
    return 200
  end
end



class Window_SkillStatus < Window_Base

  #---------------------------------------------------------------
  # * Get Window Width
  #--------------------------------------------------------------------------
  def window_width
    Graphics.width - 200
  end
end