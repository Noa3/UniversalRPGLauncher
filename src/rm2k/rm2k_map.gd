## class_name RM2KMap
## src/rm2k/rm2k_map.gd
##
## Represents a single RM2K map with tile layers, events, and metadata.
## Separate from gameplay logic — purely data representation.

extends RefCounted


## Event command for RM2K
class EventCommand:
	var code: int = 0
	var parameters: Array[int] = []
	var text: String = ""
	
	func _init(p_code: int = 0, p_params: Array[int] = Array(), p_text: String = "") -> void:
		code = p_code
		parameters = p_params
		text = p_text
	
	func to_dict() -> Dictionary:
		return {
			code = code,
			parameters = parameters,
			text = text,
		}


## Event page
class EventPage:
	var conditions: Dictionary = {}
	var commands: Array[EventCommand] = []
	var graphic: Dictionary = {}
	var trigger: int = 0  ## 0=autorun, 1=parallel, 2=action, 3=touch
	
	func to_dict() -> Dictionary:
		return {
			conditions = conditions,
			commands_count = commands.size(),
			graphic = graphic,
			trigger = trigger,
		}


## Event
class Event:
	var id: int = 0
	var x: int = 0
	var y: int = 0
	var pages: Array[EventPage] = []
	
	func _init(p_id: int = 0, p_x: int = 0, p_y: int = 0) -> void:
		id = p_id
		x = p_x
		y = p_y
	
	func to_dict() -> Dictionary:
		return {
			id = id,
			x = x,
			y = y,
			pages = [page.to_dict() for page in pages],
		}


## Tile data for a layer
class TileLayer:
	var data: PackedByteArray = PackedByteArray()
	var width: int = 0
	var height: int = 0
	
	func get_tile(p_x: int, p_y: int) -> int:
		if p_x >= 0 and p_x < width and p_y >= 0 and p_y < height:
			return data[p_y * width + p_x]
		return -1
	
	func set_tile(p_x: int, p_y: int, p_value: int) -> void:
		if p_x >= 0 and p_x < width and p_y >= 0 and p_y < height:
			data[p_y * width + p_x] = p_value


## Main map structure
var map_id: int = 0
var width: int = 0
var height: int = 0
var name: String = ""
var tileset_id: int = 0
var battleback: String = ""
var parallax: String = ""
var parallax_loop: bool = false
var parallax_loop_x: bool = false
var parallax_loop_y: bool = false
var parallax_s: int = 0
var parallax_x: int = 0
var parallax_y: int = 0

## Tile layers (lower, middle, upper)
var lower_layer: TileLayer = TileLayer.new()
var middle_layer: TileLayer = TileLayer.new()
var upper_layer: TileLayer = TileLayer.new()

## Passability layer (0=passable, 1=impassable)
var passability_layer: TileLayer = TileLayer.new()

## Events
var events: Array[Event] = []

## Map metadata
var display_name: String = ""
var encounter_list: Array[int] = []
var encounter_step: int = 0


func get_tile(p_layer: String, p_x: int, p_y: int) -> int:
	match p_layer:
		"lower": return lower_layer.get_tile(p_x, p_y)
		"middle": return middle_layer.get_tile(p_x, p_y)
		"upper": return upper_layer.get_tile(p_x, p_y)
		_: return -1
	return -1


func get_event(p_id: int) -> Event:
	for event in events:
		if event.id == p_id:
			return event
	return null


func get_events_at(p_x: int, p_y: int) -> Array[Event]:
	var result: Array[Event] = []
	for event in events:
		if event.x == p_x and event.y == p_y:
			result.append(event)
	return result


func to_dict() -> Dictionary:
	return {
		map_id = map_id,
		width = width,
		height = height,
		name = name,
		tileset_id = tileset_id,
		battleback = battleback,
		parallax = parallax,
		parallax_loop = parallax_loop,
		events = [event.to_dict() for event in events],
		display_name = display_name,
		encounter_list = encounter_list,
		encounter_step = encounter_step,
	}


func from_dict(p_dict: Dictionary) -> void:
	map_id = p_dict.get("map_id", 0)
	width = p_dict.get("width", 0)
	height = p_dict.get("height", 0)
	name = p_dict.get("name", "")
	tileset_id = p_dict.get("tileset_id", 0)
	battleback = p_dict.get("battleback", "")
	parallax = p_dict.get("parallax", "")
	parallax_loop = p_dict.get("parallax_loop", false)
	
	events = []
	for event_data in p_dict.get("events", []):
		var event := Event.new()
		event.id = event_data.get("id", 0)
		event.x = event_data.get("x", 0)
		event.y = event_data.get("y", 0)
		for page_data in event_data.get("pages", []):
			var page := EventPage.new()
			page.trigger = page_data.get("trigger", 0)
			page.conditions = page_data.get("conditions", {})
			event.pages.append(page)
		events.append(event)
	
	display_name = p_dict.get("display_name", "")
	encounter_list = p_dict.get("encounter_list", [])
	encounter_step = p_dict.get("encounter_step", 0)
