using System;
using System.IO;
using System.Linq;
using UniversalRPG.Platform;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestDirectoryScriptKeyValueStorage : TestBase
{
    public void Test_PersistsAcrossStorageObjectsWithoutExposingKeyAsFilename()
    {
        WithRoot(root =>
        {
            using(var first=new DirectoryScriptKeyValueStorage(root))
            {
                AssertTrue(first.SetItem("RPG File1","payload-日本語").Success);
            }
            var files=Directory.GetFiles(root,"*.urpgkv");
            AssertEq(files.Length,1);AssertFalse(Path.GetFileName(files[0]).Contains("RPG File1",StringComparison.Ordinal));
            using var second=new DirectoryScriptKeyValueStorage(root);
            var value=second.GetItem("RPG File1");AssertTrue(value.Success&&value.Found);AssertEq(value.Value,"payload-日本語");
        });
    }

    public void Test_EnumeratesRemovesAndClearsOwnEntries()
    {
        WithRoot(root =>
        {
            using var store=new DirectoryScriptKeyValueStorage(root);
            AssertTrue(store.SetItem("z","1").Success);AssertTrue(store.SetItem("a","2").Success);
            var keys=store.GetKeys();AssertTrue(keys.Success);AssertEq(string.Join(",",keys.Keys),"a,z");
            AssertTrue(store.RemoveItem("a").Success);AssertFalse(store.GetItem("a").Found);
            AssertTrue(store.Clear().Success);AssertEq(store.GetKeys().Keys.Count,0);
        });
    }

    public void Test_CorruptEnvelopeFailsInsteadOfReturningGarbage()
    {
        WithRoot(root =>
        {
            using var store=new DirectoryScriptKeyValueStorage(root);AssertTrue(store.SetItem("a","1").Success);
            File.WriteAllBytes(Directory.GetFiles(root,"*.urpgkv").Single(),new byte[]{1,2,3});
            var result=store.GetItem("a");AssertFalse(result.Success);AssertTrue(result.ErrorCode is "storage.corrupt" or "storage.io-failed");
        });
    }

    public void Test_UnrelatedFilesAreNotDeletedByClear()
    {
        WithRoot(root =>
        {
            File.WriteAllText(Path.Combine(root,"keep.txt"),"keep");
            using var store=new DirectoryScriptKeyValueStorage(root);AssertTrue(store.SetItem("a","1").Success);AssertTrue(store.Clear().Success);
            AssertTrue(File.Exists(Path.Combine(root,"keep.txt")));
        });
    }

    public void Test_DisposeDoesNotDeletePersistentData()
    {
        WithRoot(root =>
        {
            var store=new DirectoryScriptKeyValueStorage(root);AssertTrue(store.SetItem("a","1").Success);store.Dispose();
            using var reopened=new DirectoryScriptKeyValueStorage(root);AssertTrue(reopened.GetItem("a").Found);
        });
    }

    private static void WithRoot(Action<string> action)
    {
        var root=Path.Combine(Path.GetTempPath(),"urpg-storage-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{action(root);}finally{try{Directory.Delete(root,true);}catch{}}
    }
}
