using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kEventTriggerCodec : TestBase
{
    public void Test_RawLmuTriggerValuesMatchVerifiedLiblcfEncoding()
    {
        AssertDecoded(Rm2kEventTriggerCodec.RawAction, Rm2kEventTrigger.Action);
        AssertDecoded(Rm2kEventTriggerCodec.RawTouched, Rm2kEventTrigger.Touch);
        AssertDecoded(Rm2kEventTriggerCodec.RawCollision, Rm2kEventTrigger.Collision);
        AssertDecoded(Rm2kEventTriggerCodec.RawAutorun, Rm2kEventTrigger.Autorun);
        AssertDecoded(Rm2kEventTriggerCodec.RawParallel, Rm2kEventTrigger.Parallel);
    }

    public void Test_TriggerEncodingRoundTripsAllSupportedTriggers()
    {
        foreach (var trigger in new[]
        {
            Rm2kEventTrigger.Action,
            Rm2kEventTrigger.Touch,
            Rm2kEventTrigger.Collision,
            Rm2kEventTrigger.Autorun,
            Rm2kEventTrigger.Parallel,
        })
        {
            var raw = Rm2kEventTriggerCodec.Encode(trigger);
            AssertTrue(raw >= 0, $"{trigger} encodes to an LMU value");
            AssertTrue(Rm2kEventTriggerCodec.TryDecode(raw, out var decoded), $"{trigger} decodes after encoding");
            AssertEq(decoded, trigger, $"{trigger} round-trips");
        }
    }

    public void Test_InvalidRawTriggerFailsClosed()
    {
        AssertFalse(Rm2kEventTriggerCodec.TryDecode(-1, out _));
        AssertFalse(Rm2kEventTriggerCodec.TryDecode(5, out _));
        AssertFalse(Rm2kEventTriggerCodec.TryDecode(999, out _));
    }

    public void Test_InternalSemanticTriggerValuesRemainStable()
    {
        AssertEq((int)Rm2kEventTrigger.Autorun, 0);
        AssertEq((int)Rm2kEventTrigger.Parallel, 1);
        AssertEq((int)Rm2kEventTrigger.Action, 2);
        AssertEq((int)Rm2kEventTrigger.Touch, 3);
        AssertEq((int)Rm2kEventTrigger.Collision, 4);
    }

    private void AssertDecoded(int pRaw, Rm2kEventTrigger pExpected)
    {
        AssertTrue(Rm2kEventTriggerCodec.TryDecode(pRaw, out var decoded), $"raw trigger {pRaw} decodes");
        AssertEq(decoded, pExpected, $"raw trigger {pRaw} maps to {pExpected}");
    }
}
