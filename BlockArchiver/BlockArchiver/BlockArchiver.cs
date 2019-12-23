using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public abstract class BlockArchiver
    {
        protected const int _blockSize = 4 * 1024 * 1024;
        protected ConcurrentQueue<BlockInfo> _readBlocksQueue;
        protected ConcurrentDictionary<int, BlockInfo> _blocksToWriteQueue;
        protected ThreadsDispatcher _dispathcer;
        protected string _inputFileName;
        protected string _outputFileName;
        protected bool _isCancelled;
        protected Stopwatch _watch;
        protected long _memoryLimit;

        public event EventHandler<ProgressEventArgs> Progress;

        public BlockArchiver(string inputFileName, string outputFileName)
        {
            _dispathcer = new ThreadsDispatcher();
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _memoryLimit = (long)(new ComputerInfo().AvailablePhysicalMemory / 2);
            _readBlocksQueue = new ConcurrentQueue<BlockInfo>();
            _blocksToWriteQueue = new ConcurrentDictionary<int, BlockInfo>();
            _isCancelled = false;
            _watch = new Stopwatch();
        }
        
        public virtual long Do()
        {
            _watch.Start();

            _dispathcer.StartReadThread(ReadBlocks);
            _dispathcer.StartProcessThreads(ProcessReadBlocks);
            _dispathcer.StartWriteThread(WriteBlocks);
            _dispathcer.WaitWorkFinish();

            _watch.Stop();

            return _watch.ElapsedMilliseconds;
        }

        protected abstract void ReadBlocks();

        protected abstract void ProcessReadBlocks();

        protected virtual void WriteBlocks()
        {
            using (var outputStream = File.Open(_outputFileName, FileMode.Append))
            {
                var currentBlockNumber = 1;
                while (!_blocksToWriteQueue.IsEmpty || _dispathcer.IsReadingNotOver() || _dispathcer.IsProcessingNotOver())
                {
                    if (_blocksToWriteQueue.IsEmpty
                            && _dispathcer.IsReadingOnPause())
                    {
                        GC.Collect();
                        _dispathcer.ContinueReading();
                    }
                    else if (_blocksToWriteQueue.TryRemove(currentBlockNumber, out var tempBlock))
                    {
                        outputStream.Write(tempBlock.Data, 0, tempBlock.Data.Length);
                        Progress.Invoke(this, new ProgressEventArgs(currentBlockNumber));
                        currentBlockNumber++;
                    }
                    else
                    {
                        _dispathcer.PauseWriting();
                    }
                }
            }
        }
    }
}
