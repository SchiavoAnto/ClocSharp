using System.Text;

namespace ClocSharp;

public static class Extensions
{
    public static T[][] Partition<T>(this T[] values, int partitionCount)
    {
        if (values.Length < partitionCount) throw new InvalidOperationException();
        int totalCount = values.Length;
        int segmentLength = totalCount / partitionCount;
        int reminder = totalCount - (segmentLength * partitionCount);

        List<T[]> arrays = new();
        for (int i = 0; i < partitionCount; i++)
        {
            int amount = i == partitionCount - 1 ? (segmentLength + reminder) : segmentLength;
            int start = segmentLength * i;
            int end = start + amount;
            arrays.Add(values[start..end]);
        }

        return arrays.ToArray();
    }

    public static T Shift<T>(this T[] values)
    {
        if (values.Length == 0) throw new InvalidOperationException("The array is empty.");
        T value = values[0];
        values = values[1..];
        return value;
    }

    public static string PrettyPrint<T>(this IEnumerable<T> collection)
    {
        StringBuilder sb = new();
        sb.Append('[');
        foreach (T item in collection)
        {
            sb.Append(item?.ToString());
            sb.Append(", ");
        }
        sb.Remove(sb.Length - 3, 3);
        sb.Append(']');
        return sb.ToString();
    }
}