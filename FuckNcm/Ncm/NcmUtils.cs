using System;
using System.IO;
using System.Text;
using FuckNcm.Models;
using Newtonsoft.Json.Linq;

namespace FuckNcm.Ncm;

public static class NcmUtils
{
    // NCM 文件头
    private const string NCM_HEADER = "CTENFDAM";

    // 音频读取缓冲区大小 (32KB)
    private const int AUDIO_BUFFER_SIZE = 0x8000;

    // XOR Key
    private const byte KEY_BOX_XOR = 0x64;
    private const byte META_XOR    = 0x63;

    // 元数据跳过前缀长度 (跳过 "music:" 前缀后的 Base64 编码)
    private const int META_PREFIX_LENGTH = 22;

    // AES 解密后跳过的字节数
    private const int KEY_BOX_SKIP_BYTES = 17;

    private static readonly byte[] CoreKey =
    [
        0x68, 0x7A, 0x48, 0x52, 0x41, 0x6D, 0x73, 0x6F, 0x35, 0x6B, 0x49, 0x6E, 0x62, 0x61, 0x78, 0x57
    ];

    private static readonly byte[] MetaKey =
    [
        0x23, 0x31, 0x34, 0x6C, 0x6A, 0x6B, 0x5F, 0x21, 0x5C, 0x5D, 0x26, 0x30, 0x55, 0x3C, 0x27, 0x28
    ];

    //Header
    public static bool CheckHeader(this FileStream ms)
    {
        Span<byte> header = stackalloc byte[8];
        ms.ReadExactly(header);
        return Encoding.UTF8.GetString(header) == NCM_HEADER;
    }

    //Key Box
    public static ReadOnlySpan<byte> MakeKeyBox(this FileStream stream)
    {
        uint keyboxLen = stream.ReadUint32();

        //raw keybox data
        Span<byte> rawData = stackalloc byte[(int)keyboxLen];
        stream.ReadExactly(rawData);
        for (int i = 0; i < rawData.Length; i++)
            rawData[i] ^= KEY_BOX_XOR;

        //decrypt
        ReadOnlySpan<byte> boxData = rawData.DecryptAES(CoreKey)[KEY_BOX_SKIP_BYTES..];
        Span<byte>         keybox  = new byte[256];
        for (int i = 0; i < keybox.Length; i++)
            keybox[i] = (byte)i;

        byte lastByte = 0;
        int  offset   = 0;
        for (int i = 0; i < keybox.Length; i++)
        {
            byte c = (byte)((keybox[i] + lastByte + boxData[offset]) & 0xff);
            (keybox[i], keybox[c]) = (keybox[c], keybox[i]);

            lastByte = c;
            offset++;
            if (offset >= boxData.Length)
                offset = 0;
        }

        return keybox;
    }

    //Meta Info
    internal static NetEaseMetaInfo ReadMeta(this FileStream ms)
    {
        uint   metaLen = ms.ReadUint32();
        Span<byte> rawMeta = stackalloc byte[(int)metaLen];
        ms.ReadExactly(rawMeta);
        Span<byte> metaData = rawMeta[META_PREFIX_LENGTH..];
        for (int i = 0; i < metaData.Length; i++)
            metaData[i] ^= META_XOR;

        ReadOnlySpan<byte> decrypted = Convert.FromBase64String(Encoding.UTF8.GetString(metaData)).DecryptAES(MetaKey);
        string metaJsonStr = Encoding.UTF8.GetString(decrypted).Replace("music:", string.Empty);
        return JObject.Parse(metaJsonStr).ToObject<NetEaseMetaInfo>();
    }

    //Audio
    internal static ReadOnlyMemory<byte> ReadAudio(this FileStream ms, ReadOnlySpan<byte> keyBox)
    {
        Span<byte>         buffer      = stackalloc byte[AUDIO_BUFFER_SIZE];
        using MemoryStream audioStream = new();
        int                bytesRead;
        while ((bytesRead = ms.Read(buffer)) > 0)
        {
            Span<byte> chunk = buffer[..bytesRead];
            for (int i = 0; i < chunk.Length; i++)
            {
                int boxIndex = (i + 1) & 0xFF;
                chunk[i] ^= keyBox[(keyBox[boxIndex] + keyBox[(keyBox[boxIndex] + boxIndex) & 0xff]) & 0xff];
            }

            audioStream.Write(chunk);
        }

        return audioStream.ToArray();
    }
}