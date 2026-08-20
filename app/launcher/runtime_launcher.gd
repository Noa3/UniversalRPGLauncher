class_name RuntimeLauncher
extends RefCounted

const GameDetectorScript = preload("res://src/game_detector/game_detector.gd")

enum SupportState {
	UNAVAILABLE,
	EXPERIMENTAL,
	AVAILABLE,
}


func get_support(p_engine: int) -> Dictionary:
	match p_engine:
		GameDetectorScript.EngineType.RPGMAKER_2000, \
		GameDetectorScript.EngineType.RPGMAKER_2003, \
		GameDetectorScript.EngineType.RPGMAKER_2000_2003:
			return {
				"state": SupportState.UNAVAILABLE,
				"label": tr("RUNTIME_LCF_LABEL"),
				"reason": tr("RUNTIME_LCF_REASON"),
			}
		GameDetectorScript.EngineType.RPGMAKER_XP, \
		GameDetectorScript.EngineType.RPGMAKER_VX, \
		GameDetectorScript.EngineType.RPGMAKER_VX_ACE:
			return {
				"state": SupportState.UNAVAILABLE,
				"label": tr("RUNTIME_PLANNED_LABEL"),
				"reason": tr("RUNTIME_RGSS_REASON"),
			}
		GameDetectorScript.EngineType.RPGMAKER_MV, \
		GameDetectorScript.EngineType.RPGMAKER_MZ:
			return {
				"state": SupportState.UNAVAILABLE,
				"label": tr("RUNTIME_PLANNED_LABEL"),
				"reason": tr("RUNTIME_JS_REASON"),
			}
		_:
			return {
				"state": SupportState.UNAVAILABLE,
				"label": tr("RUNTIME_UNSUPPORTED_LABEL"),
				"reason": tr("RUNTIME_UNSUPPORTED_REASON"),
			}


func launch(p_game) -> Dictionary:
	var support := get_support(p_game.detection.engine)
	if support.state != SupportState.AVAILABLE:
		return {"success": false, "message": support.reason}
	return {"success": false, "message": tr("RUNTIME_NOT_REGISTERED")}
