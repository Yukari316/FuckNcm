using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FuckNcm.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace FuckNcm;

public static class Utils
{
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();

    public static IServiceProvider ServiceProvider => ServiceCollection.BuildServiceProvider();

    public static IThemeBackgroundService ThemeBackgroundService =>
        ServiceProvider.GetService<IThemeBackgroundService>();

    public static IStorageService StorageService => ServiceProvider.GetService<IStorageService>();

    public static ISukiDialogManager DialogManager => ServiceProvider.GetService<ISukiDialogManager>();

    public static MainWindow MainWindow => ServiceProvider.GetService<MainWindow>();

    internal static uint ReadUint32(this FileStream stream, int offset = 0)
    {
        byte[] buffer = new byte[4];
        stream.ReadExactly(buffer, offset, buffer.Length);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    internal static byte[] DecryptAES(this byte[] data, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Key  = key;
        aes.Mode = CipherMode.ECB;
        ICryptoTransform decrypter = aes.CreateDecryptor();
        return decrypter.TransformFinalBlock(data, 0, data.Length);
    }

    internal static async ValueTask<(bool success, byte[] data)> DownloadAlbumCoverAsync(string url)
    {
        HttpClient client = new();
        try
        {
            HttpResponseMessage res = await client.GetAsync(url);
            if (res.StatusCode != HttpStatusCode.OK)
                return (false, Array.Empty<byte>());
            await using Stream stream    = await res.Content.ReadAsStreamAsync();
            await using var    memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            memStream.Position = 0;
            return (true, memStream.ToArray());
        }
        catch
        {
            return (false, Array.Empty<byte>());
        }
    }

    internal static string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        fileName = invalidChars.Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        return fileName;
    }

    public static byte[] GetEmbeddedResourceStream(string resourceName)
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        using Stream stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream($"{assemblyName}.{resourceName}");
        using MemoryStream memoryStream = new();
        stream?.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}