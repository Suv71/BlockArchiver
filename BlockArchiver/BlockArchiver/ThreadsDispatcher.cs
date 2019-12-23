using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockArchiver
{
    public class ThreadsDispatcher
    {
        private Thread _readThread;
        private Thread _writeThread;
        private Thread[] _processThreads;
        private ManualResetEvent _isWritePauseEvent;
        private ManualResetEvent _isReadPauseEvent;

        public delegate void ThreadMethod();

        public ThreadsDispatcher()
        {
            _processThreads = new Thread[Environment.ProcessorCount];
            _isWritePauseEvent = new ManualResetEvent(false);
            _isReadPauseEvent = new ManualResetEvent(true);
        }

        public void StartReadThread(ThreadMethod readingMethod)
        {
            _readThread = new Thread(new ThreadStart(readingMethod));
            _readThread.Start();
        }

        public void StartWriteThread(ThreadMethod writingMethod)
        {
            _writeThread = new Thread(new ThreadStart(writingMethod));
            _writeThread.Start();
        }

        public void StartProcessThreads(ThreadMethod processMethod)
        {
            for (int i = 0; i < _processThreads.Length; i++)
            {
                _processThreads[i] = new Thread(new ThreadStart(processMethod));
                _processThreads[i].Start();
            }
        }

        public void WaitWorkFinish()
        {
            _writeThread.Join();
        }

        public bool IsReadingNotOver() => _readThread.IsAlive;

        public bool IsReadingOnPause() => !_isReadPauseEvent.WaitOne(0, false);

        public void PauseReading()
        {
            _isReadPauseEvent.Reset();
            _isReadPauseEvent.WaitOne();
        }

        public void ContinueReading() => _isReadPauseEvent.Set();

        public bool IsProcessingNotOver() => _processThreads.Any(th => th.IsAlive);

        public void PauseWriting()
        {
            _isWritePauseEvent.Reset();
            _isWritePauseEvent.WaitOne();
        }

        public void ContinueWriting() => _isWritePauseEvent.Set();
        
        
        
    }
}
