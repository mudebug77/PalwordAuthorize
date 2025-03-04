﻿using System;
using System.Text;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Core;

public static class FString
{
    private const int MaxSerializeSize = 1024;

    public static void Serialize(FArchive archive, string value, bool unicode = false)
    {
        if (unicode)
        {
            var unicodeCount = Encoding.Unicode.GetByteCount(value);
            var num = value.Length + 1;
            var saveNum = -num;
            archive.WriteInt32(saveNum);
            if (num != 0)
            {
                var valueBytesSize = unicodeCount + 2;
                var valueBytes = new byte[valueBytesSize];

                Encoding.Unicode.GetBytes(value, valueBytes);

                if (!archive.IsByteSwapping())
                {
                    archive.Serialize(valueBytes, valueBytesSize);
                }
                else
                {
                    throw new Exception("NotImplemented");
                    // for (int i = 0; i < num; i++)
                    // {
                    //     valueBytes[i] = archive.ByteSwap(valueBytes[i]);
                    // }
                }
            }
            else
            {
                archive.WriteInt32(0);
            }
        }
        else
        {
            var num = value.Length;
            if (num != 0)
            {
                // Add null terminator.
                num += 1;
                var valueBytes = new byte[num];
                Encoding.ASCII.GetBytes(value, valueBytes);

                archive.WriteInt32(num);
                archive.Serialize(valueBytes, num);
            }
            else
            {
                archive.WriteInt32(0);
            }
        }

    }
    
    public static string Deserialize(FArchive archive)
    {
        // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization

        string? result = null;

        var saveNum = archive.ReadInt32();
        var loadUcs2Char = saveNum < 0;
        if (loadUcs2Char)
        {
            saveNum = -saveNum;
        }

        // If SaveNum is still less than 0, they must have passed in MIN_INT. Archive is corrupted.
        if (saveNum < 0)
        {
            throw new Exception("Archive is corrupted");
        }

        // Protect against network packets allocating too much memory
        if (MaxSerializeSize > 0 && saveNum > MaxSerializeSize)
        {
            throw new Exception("String is too large");
        }

        if (saveNum != 0)
        {
            if (loadUcs2Char)
            {
                var bytes = archive.ReadBytes(saveNum * 2);

                // -2 to remove unicode null terminator.
                result = Encoding.Unicode.GetString(bytes, 0, bytes.Length - 2);
            }
            else
            {
                var bytes = archive.ReadBytes(saveNum);

                // -1 to remove null terminator.
                result = Encoding.ASCII.GetString(bytes, 0, bytes.Length - 1);
            }

            // Throw away empty string.
            if (saveNum == 1)
            {
                result = string.Empty;
            }
        }

        return result ?? string.Empty;
    }
}