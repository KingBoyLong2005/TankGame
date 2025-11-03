using System;
using Unity.Collections;
using Unity.Netcode;

// Struct để lưu thông tin bot
public struct BotData : INetworkSerializable, IEquatable<BotData>
{
    public FixedString64Bytes botName;
    public int skinIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref botName);
        serializer.SerializeValue(ref skinIndex);
    }

    public bool Equals(BotData other)
    {
        return botName.Equals(other.botName) && skinIndex == other.skinIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is BotData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(botName, skinIndex);
    }
}