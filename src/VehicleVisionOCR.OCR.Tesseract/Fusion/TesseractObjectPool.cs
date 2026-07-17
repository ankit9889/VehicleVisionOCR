using System;
using System.Collections.Concurrent;
using Tesseract;

namespace VehicleVisionOCR.OCR.Tesseract.Fusion
{
    public class TesseractObjectPool : IDisposable
    {
        private readonly ConcurrentQueue<TesseractEngine> _pool = new ConcurrentQueue<TesseractEngine>();
        private readonly SemaphoreSlim _semaphore;
        private readonly string _tessdataPath;

        public TesseractObjectPool(string tessdataPath = "./tessdata", int? maxDegreeOfParallelism = null)
        {
            _tessdataPath = tessdataPath;
            int maxEngines = maxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount - 1);
            _semaphore = new SemaphoreSlim(maxEngines, maxEngines);
        }

        private TesseractEngine CreateInstance()
        {
            return new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
        }

        public async System.Threading.Tasks.Task<TesseractEngine> BorrowAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_pool.TryDequeue(out var engine))
            {
                return engine;
            }
            
            return CreateInstance();
        }

        public void Return(TesseractEngine engine)
        {
            if (engine != null && !engine.IsDisposed)
            {
                // Reset common variables before returning
                engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                engine.SetVariable("tessedit_char_whitelist", "");
                _pool.Enqueue(engine);
            }
            _semaphore.Release();
        }

        public void Dispose()
        {
            while (_pool.TryDequeue(out var engine))
            {
                engine.Dispose();
            }
        }
    }
}
