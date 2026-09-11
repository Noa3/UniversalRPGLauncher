using System;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

public partial class TestPluginDetection
{
    public void Test_Rm2kPassabilityExposesUpperLayerCounterFlags()
    {
        var upper = new byte[Rm2kPassabilityMap.UpperPassageCount];
        Array.Fill(upper, (byte)0x0F);
        upper[0] |= Rm2kPassabilityMap.Counter;

        var database = PassabilityDatabase(1, null, upper);
        var map = PassabilityMap(
            2, 1, 1,
            new[] { 2000, 2000 },
            new[] { 10000, 10001 });

        AssertTrue(Rm2kPassabilityMap.TryCreate(database, map, out var passability, out var error), error);
        AssertTrue(passability != null);
        AssertTrue(passability!.IsCounter(0, 0), "counter flag is read from upper passage entry");
        AssertFalse(passability.IsCounter(1, 0), "neighbor without counter flag is not a counter");
        AssertFalse(passability.IsCounter(-1, 0), "out-of-bounds coordinate is never a counter");
        AssertFalse(passability.IsCounter(2, 0), "out-of-bounds coordinate is never a counter");
    }
}
