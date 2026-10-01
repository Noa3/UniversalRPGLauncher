#==============================================================================
# This script has been altered by Kslander for the need of LonaRPG
#==============================================================================
#==============================================================================
# ** Scene_Shop
#------------------------------------------------------------------------------
#  This class performs shop screen processing.
#==============================================================================

class Scene_Shop < Scene_MenuBase
  #--------------------------------------------------------------------------
  # * Prepare
  #--------------------------------------------------------------------------
  def prepare(goods, purchase_only)
    @goods = goods
    @purchase_only = purchase_only
  end
  #--------------------------------------------------------------------------
  # * Start Processing
  #--------------------------------------------------------------------------
  def start
    super
    create_help_window
    create_gold_window
    create_command_window
	create_category_window
    create_dummy_window
    create_number_window
    create_buy_window
    create_sell_window
	modify_help_window
  end
  
  def modify_help_window
	@help_window.y = @category_window.height
	@help_window.x = @buy_window.width
	@help_window.width =Graphics.width -  @buy_window.width
	@help_window.height= @dummy_window.height
	@help_window.create_contents
	@help_window.hide
  end
  #--------------------------------------------------------------------------
  # * Create Gold Window
  #--------------------------------------------------------------------------
  def create_gold_window
    @gold_window = Window_Gold.new
    @gold_window.viewport = @viewport
    @gold_window.x = Graphics.width - @gold_window.width
    @gold_window.y = 0 #@help_window.height
  end
  #--------------------------------------------------------------------------
  # * Create Command Window
  #--------------------------------------------------------------------------
  def create_command_window
    @command_window = Window_ShopCommand.new(@gold_window.x, @purchase_only)
    @command_window.viewport = @viewport
    @command_window.y = 0#@category_window.height #@help_window.height
    @command_window.set_handler(:buy,    method(:command_buy))
    @command_window.set_handler(:sell,   method(:command_sell))
    @command_window.set_handler(:cancel, method(:return_map))
  end
  #--------------------------------------------------------------------------
  # * Create Dummy Window
  #--------------------------------------------------------------------------
  def create_dummy_window
    #wy = @category_window.y + @category_window.height
    wy = @command_window.y + @command_window.height
    wh = Graphics.height - wy
    @dummy_window = Window_Base.new(0, wy, Graphics.width, wh)
    @dummy_window.viewport = @viewport
  end
  #--------------------------------------------------------------------------
  # * Create Quantity Input Window
  #--------------------------------------------------------------------------
  def create_number_window
    wy = @dummy_window.y
    wh = @dummy_window.height
    @number_window = Window_ShopNumber.new(0, wy, wh)
    @number_window.viewport = @viewport
    @number_window.hide
    @number_window.set_handler(:ok,     method(:on_number_ok))
    @number_window.set_handler(:cancel, method(:on_number_cancel))
  end

  #--------------------------------------------------------------------------
  # * Create Purchase Window
  #--------------------------------------------------------------------------
  def create_buy_window
    wy = @dummy_window.y
    wh = @dummy_window.height
    @buy_window = Window_ShopBuy.new(0, wy, wh, @goods)
    @buy_window.viewport = @viewport
    @buy_window.help_window = @help_window
    @buy_window.hide
    @buy_window.set_handler(:ok,     method(:on_buy_ok))
    @buy_window.set_handler(:cancel, method(:on_buy_cancel))
  end
  #--------------------------------------------------------------------------
  # * Create Category Window
  #--------------------------------------------------------------------------
  def create_category_window
    @category_window = Window_ItemCategory.new
    @category_window.viewport = @viewport
    @category_window.help_window = @help_window
    @category_window.y = @command_window.height
    @category_window.hide.deactivate
    @category_window.set_handler(:ok,     method(:on_category_ok))
    @category_window.set_handler(:cancel, method(:on_category_cancel))
  end
  #--------------------------------------------------------------------------
  # * Create Sell Window
  #--------------------------------------------------------------------------
  def create_sell_window
    wy = @category_window.y + @category_window.height
    wh = Graphics.height - wy
    @sell_window = Window_ShopSell.new(0, wy, Graphics.width, wh)
    @sell_window.viewport = @viewport
    @sell_window.help_window = @help_window
    @sell_window.hide
    @sell_window.set_handler(:ok,     method(:on_sell_ok))
    @sell_window.set_handler(:cancel, method(:on_sell_cancel))
    @category_window.item_window = @sell_window
  end
  #--------------------------------------------------------------------------
  # * Activate Purchase Window
  #--------------------------------------------------------------------------
  def activate_buy_window
    @buy_window.money = money
    @buy_window.show.activate
	@help_window.height = Graphics.height - @category_window.height
	@help_window.y = @buy_window.y
	@help_window.show
  end
  #--------------------------------------------------------------------------
  # * Activate Sell Window
  #--------------------------------------------------------------------------
  def activate_sell_window
    @category_window.show
    @sell_window.refresh
    @sell_window.show.activate
	@help_window.hide
  end
  #--------------------------------------------------------------------------
  # * [Buy] Command
  #--------------------------------------------------------------------------
  def command_buy
    @dummy_window.hide
    activate_buy_window
  end
  #--------------------------------------------------------------------------
  # * [Sell] Command
  #--------------------------------------------------------------------------
  def command_sell
    @dummy_window.hide
    @category_window.show.activate
    @sell_window.show
    @sell_window.unselect
    @sell_window.refresh
	@help_window.hide
  end
  #--------------------------------------------------------------------------
  # * Buy [OK]
  #--------------------------------------------------------------------------
  def on_buy_ok
    @item = @buy_window.item
    @buy_window.hide
    @number_window.set(@item, max_buy, buying_price, currency_unit)
    @number_window.show.activate
  end
  #--------------------------------------------------------------------------
  # * Buy [Cancel]
  #--------------------------------------------------------------------------
  def on_buy_cancel
    @command_window.activate
    @dummy_window.show
    @buy_window.hide
    @help_window.hide
    @help_window.clear
  end
  #--------------------------------------------------------------------------
  # * Category [OK]
  #--------------------------------------------------------------------------
  def on_category_ok
    activate_sell_window
    @sell_window.select(0)
  end
  #--------------------------------------------------------------------------
  # * Category [Cancel]
  #--------------------------------------------------------------------------
  def on_category_cancel
    @command_window.activate
    @dummy_window.show
    @category_window.hide
    @sell_window.hide
  end
  #--------------------------------------------------------------------------
  # * Sell [OK]
  #--------------------------------------------------------------------------
  def on_sell_ok
    @item = @sell_window.item
    @category_window.hide
    @sell_window.hide
	@help_window.width= Graphics.width - @number_window.width
	@help_window.height = @number_window.height
	@help_window.y= @number_window.y
	@help_window.x = @number_window.width
	@help_window.show
    @number_window.set(@item, max_sell, @item.get_sell_price, currency_unit)
    @number_window.show.activate
  end
  #--------------------------------------------------------------------------
  # * Sell [Cancel]
  #--------------------------------------------------------------------------
  def on_sell_cancel
    @sell_window.unselect
    @category_window.activate
	@help_window.hide
    @help_window.clear
  end
  #--------------------------------------------------------------------------
  # * Quantity Input [OK]
  #--------------------------------------------------------------------------
  def on_number_ok
    SndLib.sys_equip
    case @command_window.current_symbol
    when :buy
      do_buy(@number_window.number)
    when :sell
      do_sell(@number_window.number)
    end
    end_number_input
    @gold_window.refresh
  end
  #--------------------------------------------------------------------------
  # * Quantity Input [Cancel]
  #--------------------------------------------------------------------------
  def on_number_cancel
    SndLib.sys_cancel
    end_number_input
  end
  #--------------------------------------------------------------------------
  # * Execute Purchase
  #--------------------------------------------------------------------------
  def do_buy(number)
    $game_party.lose_gold(number * buying_price)
    $game_party.gain_item(@item, number)
  end
  #--------------------------------------------------------------------------
  # * Execute Sale
  #--------------------------------------------------------------------------
  def do_sell(number)
    $game_party.gain_gold(number * @item.get_sell_price)
    $game_party.lose_item(@item, number)
  end
  #--------------------------------------------------------------------------
  # * Exit Quantity Input
  #--------------------------------------------------------------------------
  def end_number_input
    @number_window.hide
    case @command_window.current_symbol
    when :buy
      activate_buy_window
    when :sell
      activate_sell_window
    end
  end
  #--------------------------------------------------------------------------
  # * Get Maximum Quantity Buyable
  #--------------------------------------------------------------------------
  def max_buy
    max = $game_party.max_item_number(@item) - $game_party.item_number(@item)
    buying_price == 0 ? max : [max, money / buying_price].min
  end
  #--------------------------------------------------------------------------
  # * Get Maximum Quantity Sellable
  #--------------------------------------------------------------------------
  def max_sell
    $game_party.item_number(@item)
  end
  #--------------------------------------------------------------------------
  # * Get Party Gold
  #--------------------------------------------------------------------------
  def money
    @gold_window.value
  end
  #--------------------------------------------------------------------------
  # Get Currency Unit
  #--------------------------------------------------------------------------
  def currency_unit
    @gold_window.currency_unit
  end
  #--------------------------------------------------------------------------
  # * Get Purchase Price
  #--------------------------------------------------------------------------
  def buying_price
    @buy_window.price(@item)
  end
  #--------------------------------------------------------------------------
  # * Get Sale Price
  #--------------------------------------------------------------------------
  #def selling_price
	#case @item.type_tag
	#	when "Money"
	#		temp_wisdom_fix = $game_player.actor.wisdom*0.001
	#		temp_final_price = (@item.price * [0.9+temp_wisdom_fix,0.95].min).round
	#		temp_final_price
	#	else
	#		temp_wisdom_fix = $game_player.actor.wisdom*0.005
	#		temp_final_price = (@item.price * [0.5+temp_wisdom_fix,0.95].min).round
	#		temp_final_price
	#	end
  #end
end
