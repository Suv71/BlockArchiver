using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class BlockArchiver
    {
        private int _blockSize;
        private ConcurrentQueue<byte[]> _readBlocksQueue;
        private ConcurrentQueue<byte[]> _blocksToWriteQueue;
        private Thread _readThread;
        private Thread _writeThread;
        private Thread[] _workThreads;
        private string _inputFileName;
        private string _outputFileName;
        private bool _isCancelled;
        private ManualResetEvent _readCompletedEvent;
        private ManualResetEvent _writeCompletedEvent;

        public BlockArchiver()
        {
            _blockSize = 1024 * 1024; // 1 Мб
            _readBlocksQueue = new ConcurrentQueue<byte[]>();
            _blocksToWriteQueue = new ConcurrentQueue<byte[]>();
            _workThreads = new Thread[Environment.ProcessorCount];
            _isCancelled = false;
            _readCompletedEvent = new ManualResetEvent(false);
            _writeCompletedEvent = new ManualResetEvent(false);
        }

        public void CompressFile(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _readThread = new Thread(ReadOriginalBlocks);
            _readThread.Start();

            for (int i = 0; i < _workThreads.Length; i++)
            {
                _workThreads[i] = new Thread(CompressBlock);
                _workThreads[i].Start();
            }

            _writeThread = new Thread(WriteBlocks);
            _writeThread.Start();

            WaitHandle.WaitAll(new WaitHandle[] {_writeCompletedEvent });
        }

        private void ReadOriginalBlocks()
        {
            using (var inputStream = File.OpenRead(_inputFileName))
            {
                byte[] readBlock;
                while (inputStream.Position < inputStream.Length)
                {
                    if (inputStream.Length - inputStream.Position >= _blockSize)
                    {
                        readBlock = new byte[_blockSize];
                    }
                    else
                    {
                        readBlock = new byte[inputStream.Length - inputStream.Position];
                    }
                    inputStream.Read(readBlock, 0, readBlock.Length);
                    _readBlocksQueue.Enqueue(readBlock);
                }
                _readCompletedEvent.Set();
            }
        }

        private void CompressBlock()
        {
            while (!_readBlocksQueue.IsEmpty)
            {
                _readBlocksQueue.TryDequeue(out var tempBlock);

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gzipStream.Write(tempBlock, 0, tempBlock.Length);
                    }
                    var compressedBlock = memoryStream.ToArray();
                    var compressedBlockLengthInBytes = BitConverter.GetBytes(compressedBlock.Length);
                    compressedBlockLengthInBytes.CopyTo(compressedBlock, 4);
                    _blocksToWriteQueue.Enqueue(compressedBlock);
                }
            }
        }

        private void WriteBlocks()
        {
            while (!_blocksToWriteQueue.IsEmpty || !_readCompletedEvent.WaitOne(0, false) || _workThreads.Any(th => th.IsAlive))
            {
                if (!_blocksToWriteQueue.IsEmpty)
                {
                    _blocksToWriteQueue.TryDequeue(out var tempBlock);

                    using (var outputStream = File.Open(_outputFileName, FileMode.Append))
                    {
                        outputStream.Write(tempBlock, 0, tempBlock.Length);
                    }
                }
            }
            _writeCompletedEvent.Set();
        }
    }
}
