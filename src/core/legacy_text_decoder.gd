class_name LegacyTextDecoder
extends RefCounted

# Godot's multibyte decoder accepts SHIFT_JIS on Windows. CP932 and SJIS are
# common aliases used by game metadata and are normalized before this list is
# attempted so they do not produce an avoidable engine error first.
const JAPANESE_ENCODINGS: Array[String] = ["SHIFT_JIS"]


func decode(p_bytes: PackedByteArray, p_preferred_encoding: String = "") -> String:
	if p_bytes.is_empty():
		return ""
	if p_bytes.size() >= 3 and p_bytes[0] == 0xef and p_bytes[1] == 0xbb and p_bytes[2] == 0xbf:
		return p_bytes.slice(3).get_string_from_utf8()
	if p_bytes.size() >= 2 and p_bytes[0] == 0xff and p_bytes[1] == 0xfe:
		return p_bytes.slice(2).get_string_from_utf16()

	if _is_valid_utf8(p_bytes):
		return p_bytes.get_string_from_utf8()

	var encodings: Array[String] = []
	if not p_preferred_encoding.is_empty():
		encodings.append(_normalize_encoding(p_preferred_encoding))
	for encoding in JAPANESE_ENCODINGS:
		if encoding not in encodings:
			encodings.append(encoding)
	for encoding in encodings:
		var decoded := p_bytes.get_string_from_multibyte_char(encoding)
		if not decoded.is_empty():
			return decoded
	return ""


func _normalize_encoding(p_encoding: String) -> String:
	var normalized := p_encoding.strip_edges().to_upper().replace("-", "_")
	if normalized in ["CP932", "SJIS", "SHIFTJIS", "SHIFT_JIS"]:
		return "SHIFT_JIS"
	return p_encoding


func _is_valid_utf8(p_bytes: PackedByteArray) -> bool:
	var index := 0
	while index < p_bytes.size():
		var first := p_bytes[index]
		if first <= 0x7f:
			index += 1
			continue
		var continuation_count := 0
		if first >= 0xc2 and first <= 0xdf:
			continuation_count = 1
		elif first >= 0xe0 and first <= 0xef:
			continuation_count = 2
		elif first >= 0xf0 and first <= 0xf4:
			continuation_count = 3
		else:
			return false
		if index + continuation_count >= p_bytes.size():
			return false
		var second := p_bytes[index + 1]
		if second < 0x80 or second > 0xbf:
			return false
		if first == 0xe0 and second < 0xa0:
			return false
		if first == 0xed and second > 0x9f:
			return false
		if first == 0xf0 and second < 0x90:
			return false
		if first == 0xf4 and second > 0x8f:
			return false
		for offset in range(2, continuation_count + 1):
			var continuation := p_bytes[index + offset]
			if continuation < 0x80 or continuation > 0xbf:
				return false
		index += continuation_count + 1
	return true
