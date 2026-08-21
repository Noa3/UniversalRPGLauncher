## tests/core/test_legacy_text_decoder.gd
## Regression tests for legacy Japanese metadata decoding.

extends Test

var decoder: LegacyTextDecoder


func setup() -> void:
	decoder = LegacyTextDecoder.new()


func test_default_japanese_candidates_use_godot_supported_encoding_name() -> void:
	assert_eq(decoder.JAPANESE_ENCODINGS, ["SHIFT_JIS"])


func test_cp932_alias_still_decodes_as_shift_jis() -> void:
	var bytes := PackedByteArray([0x83, 0x65, 0x83, 0x58, 0x83, 0x67])
	assert_eq(decoder.decode(bytes, "CP932"), "テスト")


func test_valid_utf8_does_not_use_legacy_conversion() -> void:
	assert_eq(decoder.decode("Über UTF-8".to_utf8_buffer()), "Über UTF-8")
