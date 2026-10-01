def r
  return if @health_bar.src_rect.width == (@ratio * @actor.health).ceil + 1
end
