using ATL;
using FuckNcm.Models;
using FuckNcm.Ncm;

namespace FuckNcm.UnitTest;

[TestClass]
public sealed class NcmTest
{
    private const string NCM_FILE_PATH = @"G:\Desktop\DevWorkDir\NCM\鸣潮先约电台,jixwang,Laco - 于无羁之昼点亮真彩（Throttle Up!).ncm";

    private const string AUDIO_FILE_PATH = @"G:\Desktop\DevWorkDir\NCM\40mP,初音ミク,GUMI - Smile again.flac";

    private const string OUT_PATH = @"G:\Desktop\DevWorkDir\NCM-out";

    [TestMethod]
    public void TestIsNcm()
    {
        FileStream ncmFileStream = new(NCM_FILE_PATH, FileMode.Open, FileAccess.Read);
        Assert.IsNotNull(ncmFileStream);
        Assert.IsTrue(ncmFileStream.CheckHeader());
    }

    [TestMethod]
    public void TestIsNotNcm()
    {
        FileStream audioFileStream = new(AUDIO_FILE_PATH, FileMode.Open, FileAccess.Read);
        Assert.IsNotNull(audioFileStream);
        Assert.IsFalse(audioFileStream.CheckHeader());
    }

    [TestMethod]
    public async Task TestNcmDecryption()
    {
        FileStream ncmFileStream = new(NCM_FILE_PATH, FileMode.Open, FileAccess.Read);
        Assert.IsNotNull(ncmFileStream);
        Assert.IsTrue(ncmFileStream.CheckHeader());
        (NcmParseStatus status, Track track) = await NcmDump.DumpNcmFile(NCM_FILE_PATH, OUT_PATH);
        Assert.AreEqual(NcmParseStatus.OK, status);
    }
}