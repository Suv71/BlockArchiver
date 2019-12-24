using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace BlockArchiver
{
    public abstract class BlockArchiver
    {
        protected const int _intBlockLength = 4;
        protected const int _blockSize = 6 * 1024 * 1024;
        protected ConcurrentQueue<BlockInfo> _readBlocks;
        protected ConcurrentDictionary<int, BlockInfo> _blocksToWrite;
        protected ThreadsDispatcher _dispathcer;
        protected string _inputFileName;
        protected string _outputFileName;
        protected bool _isCancelled;
        protected Stopwatch _watch;
        
        public event EventHandler<ProgressEventArgs> Progress;

        public BlockArchiver(string inputFileName, string outputFileName)
        {
            _dispathcer = new ThreadsDispatcher();
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _readBlocks = new ConcurrentQueue<BlockInfo>();
            _blocksToWrite = new ConcurrentDictionary<int, BlockInfo>();
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
            using (var outputStream = File.Create(_outputFileName))
            {
                var currentBlockNumber = 1;
                while (!_blocksToWrite.IsEmpty || _dispathcer.IsReadingNotOver() || _dispathcer.IsProcessingNotOver())
                {
                    if (_dispathcer.IsReadingOnPause() && _readBlocks.IsEmpty)
                    {
                        GC.Collect();
                        _dispathcer.ContinueReading();
                    }

                    if (_blocksToWrite.TryRemove(currentBlockNumber, out var tempBlock))
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
