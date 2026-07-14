using System.Threading.Channels;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Backend.BackgroundServices
{
    public class QueuedImage
    {
        public string ScannerId { get; set; } = string.Empty;
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
    }

    public class ImageProcessingQueue
    {
        private readonly Channel<QueuedImage> _queue;

        public ImageProcessingQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<QueuedImage>(options);
        }

        public async ValueTask EnqueueAsync(string scannerId, byte[] imageBytes)
        {
            await _queue.Writer.WriteAsync(new QueuedImage { ScannerId = scannerId, ImageData = imageBytes });
        }

        public async ValueTask<QueuedImage> DequeueAsync(System.Threading.CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
