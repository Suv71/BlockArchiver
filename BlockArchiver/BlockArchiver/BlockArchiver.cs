using System;
using System.Collections.Concurrent;
using System.IO;

namespace BlockArchiver
{
    public abstract class BlockArchiver
    {
        protected const int _int32BlockLength = 4;
        protected const int _int64BlockLength = 8;
        protected const int _blockLength = 6 * 1024 * 1024;
        protected ConcurrentQueue<BlockInfo> _readBlocks;
        protected ConcurrentDictionary<int, BlockInfo> _blocksToWrite;
        protected Dispatcher _dispathcer;
        protected string _inputFileName;
        protected string _outputFileName;
        protected long _uncompressedFileLength; 
        protected int _totalBlockNumber;
        protected bool _isCancelled;
        
        public event EventHandler<ProgressEventArgs> Progress;
        public event EventHandler<ErrorEventArgs> Error;

        public BlockArchiver(string inputFileName, string outputFileName)
        {
            _dispathcer = new Dispatcher();
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _readBlocks = new ConcurrentQueue<BlockInfo>();
            _blocksToWrite = new ConcurrentDictionary<int, BlockInfo>();
            _isCancelled = false;
        }

        public virtual int Do()
        {
            _dispathcer.StartReadThread(ReadBlocks);
            _dispathcer.StartProcessThreads(ProcessReadBlocks);
            _dispathcer.StartWriteThread(WriteBlocks);
            _dispathcer.WaitWorkFinish();
            return _isCancelled ? 1 : 0;
        }

        public void Cancel()
        {
            _dispathcer.ActivatePausedThreads();
            _isCancelled = true;
        }

        protected abstract void ReadBlocks();

        protected abstract void ProcessReadBlocks();

        protected virtual void WriteBlocks()
        {
            try
            {
                //using (var outputStream = File.Create(_outputFileName))
                using (var outputStream = new FileStream(_outputFileName, FileMode.Create, FileAccess.Write, FileShare.Write, _blockLength))
                {
                    WriteUncompressedFileLength(outputStream);
                    var currentBlockNumber = 1;
                    while (!_isCancelled && (!_blocksToWrite.IsEmpty || _dispathcer.IsReadingNotOver() || _dispathcer.IsProcessingNotOver()))
                    {
                        if (_dispathcer.IsReadingOnPause() && _readBlocks.IsEmpty)
                        {
                            //GC.Collect();
                            _dispathcer.ContinueReading();
                        }

                        if (_blocksToWrite.TryRemove(currentBlockNumber, out var currentBlockInfo))
                        {
                            outputStream.Write(currentBlockInfo.Data, 0, currentBlockInfo.Data.Length);
                            OnProgress(new ProgressEventArgs(currentBlockNumber, _totalBlockNumber));
                            currentBlockNumber++;
                        }
                        else
                        {
                            _dispathcer.PauseWriting();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Возникла ошибка при записи блоков в файл: {ex.Message}", ex));
            }
        }

        protected virtual void WriteUncompressedFileLength(FileStream outputStream)
        {
        }

        protected void SetTotalBlockNumber()
        {
            _totalBlockNumber = (int)(_uncompressedFileLength / _blockLength);
            if (_uncompressedFileLength % _blockLength != 0)
            {
                _totalBlockNumber += 1;
            }
        }

        protected void OnError(ErrorEventArgs args)
        {
            _isCancelled = true;
            Error?.Invoke(this, args);
        }

        protected void OnProgress(ProgressEventArgs args)
        {
            Progress?.Invoke(this, args);
        }
    }
}
