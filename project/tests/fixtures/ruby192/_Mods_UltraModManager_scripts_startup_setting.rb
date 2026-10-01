# frozen_string_literal: true

class Setting
  attr_accessor :setting_id, :localized_name, :possible_values, :value, :callback

  def initialize(setting_id, localized_name, possible_values, init_value, callback)
    @setting_id = setting_id
    @localized_name = localized_name
    @possible_values = possible_values
    @value = init_value
    @callback = callback
  end
end
