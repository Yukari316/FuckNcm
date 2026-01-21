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

    private static Lazy<IServiceProvider> ServiceProviderLazy = new(() => ServiceCollection.BuildServiceProvider());

    private static readonly Lazy<HttpClient> HttpClientLazy = new(() => new HttpClient());

    public static IServiceProvider ServiceProvider => ServiceProviderLazy.Value;

    public static IThemeBackgroundService ThemeBackgroundService =>
        ServiceProvider.GetRequiredService<IThemeBackgroundService>();

    public static IStorageService StorageService => ServiceProvider.GetRequiredService<IStorageService>();

    public static ISukiDialogManager DialogManager => ServiceProvider.GetRequiredService<ISukiDialogManager>();

    public static MainWindow MainWindow => ServiceProvider.GetService<MainWindow>();

    public static void RebuildServiceProvider() =>
        ServiceProviderLazy = new Lazy<IServiceProvider>(() => ServiceCollection.BuildServiceProvider());

    internal static uint ReadUint32(this FileStream stream, int offset = 0)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    internal static ReadOnlySpan<byte> DecryptAES(this ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        using Aes aes = Aes.Create();
        aes.Key  = key.ToArray();
        aes.Mode = CipherMode.ECB;
        return aes.DecryptEcb(data, PaddingMode.PKCS7);
    }

    internal static async ValueTask<(bool success, byte[] data)> DownloadAlbumCoverAsync(string url)
    {
        try
        {
            HttpResponseMessage res = await HttpClientLazy.Value.GetAsync(url);
            if (res.StatusCode != HttpStatusCode.OK)
                return (false, Array.Empty<byte>());
            await using Stream       stream    = await res.Content.ReadAsStreamAsync();
            await using MemoryStream memStream = new();
            await stream.CopyToAsync(memStream);
            memStream.Position = 0;
            return (true, memStream.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载封面失败: {ex.Message}");
            return (false, []);
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