namespace FashionStore.API.Features.Products.UploadProductImage;

internal sealed class LimitedReadStream(Stream inner, long maximumLength) : Stream
{
    private long totalRead;

    public override bool CanRead
    {
        get
        {
            return inner.CanRead;
        }
    }

    public override bool CanSeek
    {
        get
        {
            return false;
        }
    }

    public override bool CanWrite
    {
        get
        {
            return false;
        }
    }

    public override long Length
    {
        get
        {
            return totalRead;
        }
    }

    public override long Position
    {
        get
        {
            return totalRead;
        }
        set
        {
            throw new NotSupportedException();
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer.AsMemory(offset, count)).GetAwaiter().GetResult();
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var remaining = maximumLength + 1 - totalRead;
        if (remaining <= 0)
        {
            throw new ArgumentException("Image cannot exceed 5 MB.");
        }

        var read = await inner.ReadAsync(
            buffer[..(int)Math.Min(buffer.Length, remaining)],
            cancellationToken);
        totalRead += read;
        if (totalRead > maximumLength)
        {
            throw new ArgumentException("Image cannot exceed 5 MB.");
        }

        return read;
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
