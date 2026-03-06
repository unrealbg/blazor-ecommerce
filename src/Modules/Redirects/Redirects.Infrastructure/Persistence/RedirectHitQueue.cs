using System.Threading.Channels;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectHitQueue
{
    private readonly Channel<RedirectHitRecord> channel = Channel.CreateBounded<RedirectHitRecord>(
        new BoundedChannelOptions(8192)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelReader<RedirectHitRecord> Reader => channel.Reader;

    public bool TryWrite(RedirectHitRecord record)
    {
        return channel.Writer.TryWrite(record);
    }
}
