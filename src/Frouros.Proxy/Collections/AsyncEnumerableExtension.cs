namespace Frouros.Proxy.Collections;

public static class AsyncEnumerableExtension
{
    public static async Task<IEnumerable<T>> AsTask<T>(this IAsyncEnumerable<T> enumerable)
    {
        var list = new Queue<T>();
        
        await using var iter = enumerable.GetAsyncEnumerator();
        while (await iter.MoveNextAsync())
        {
            list.Enqueue(iter.Current);
        }

        return list;
    }
}