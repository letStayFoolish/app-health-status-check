namespace DeepCheck.Helpers;

using System.Threading.Tasks;
using PuppeteerSharp;

public record ElementHandlesDisposable(IElementHandle[] Handles) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        foreach (var handle in Handles)
        {
            try
            {
                await handle.DisposeAsync();
            }
            catch
            {
                /* ignore dispose errors */
            }
        }
    }
}
