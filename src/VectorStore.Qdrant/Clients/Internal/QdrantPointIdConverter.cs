using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Qdrant.Client.Grpc;

namespace VectorStore.Qdrant.Clients.Internal;

internal static class QdrantPointIdConverter
{
    public static PointId ToPointId(string chunkId)
    {
        if (ulong.TryParse(
                chunkId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericId))
        {
            return numericId;
        }

        if (Guid.TryParse(chunkId, out var guidId))
        {
            return guidId;
        }

        return CreateStableGuid(chunkId);
    }

    public static string ToChunkId(PointId pointId)
    {
        if (pointId.HasUuid)
        {
            return pointId.Uuid;
        }

        if (pointId.HasNum)
        {
            return pointId.Num.ToString(CultureInfo.InvariantCulture);
        }

        return string.Empty;
    }

    private static Guid CreateStableGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }
}
