using System.Text;
using UniversalRPG.Core;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestLegacyTextDecoder : TestBase
{
	private LegacyTextDecoder _decoder = null!;

	public override void Setup()
	{
		_decoder = new LegacyTextDecoder();
	}

	public void Test_DefaultJapaneseCandidatesUseGodotSupportedEncodingName()
	{
		AssertEq(string.Join(",", LegacyTextDecoder.JapaneseEncodings), "SHIFT_JIS");
	}

	public void Test_Cp932AliasStillDecodesAsShiftJis()
	{
		byte[] bytes = { 0x83, 0x65, 0x83, 0x58, 0x83, 0x67 };
		AssertEq(_decoder.Decode(bytes, "CP932"), "テスト");
	}

	public void Test_ValidUtf8DoesNotUseLegacyConversion()
	{
		AssertEq(_decoder.Decode(Encoding.UTF8.GetBytes("Über UTF-8")), "Über UTF-8");
	}
}
