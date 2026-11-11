namespace KiaiControl.UseCases.Common;
public static class PagedOptions
{
    public static async Task<IReadOnlyList<T>> ReadAllAsync<T>(Func<int, Task<IReadOnlyList<T>>> readPage)
    {
        var result = new List<T>();
        for (var page = 1;; page++)
        {
            var batch = await readPage(page);
            result.AddRange(batch);
            if (batch.Count < 200)
                return result;
        }
    }
}
