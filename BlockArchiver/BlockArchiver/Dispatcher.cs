using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BlockArchiver
{
    public class Dispatcher
    {
        private Thread _readThread;
        private Thread _writeThread;
        private Thread[] _processThreads;
        private ManualResetEvent _writePauseEvent;
        private ManualResetEvent _readPauseEvent;
        private Process _currentProcess;
        private long _memoryLimit;

        public delegate void ThreadMethod();

        public Dispatcher()
        {
            _processThreads = new Thread[Environment.ProcessorCount];
            _writePauseEvent = new ManualResetEvent(false);
            _readPauseEvent = new ManualResetEvent(true);
            _currentProcess = Process.GetCurrentProcess();
            _memoryLimit = GetMemoryLimit();
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
            _readThread?.Join();
            foreach (var thread in _processThreads)
            {
                thread?.Join();
            }
            _writeThread?.Join();
        }

        public bool IsReadingNotOver() => _readThread?.IsAlive ?? false;

        public bool IsReadingOnPause() => !_readPauseEvent.WaitOne(0, false);

        public void PauseReading()
        {
            _readPauseEvent.Reset();
            _readPauseEvent.WaitOne();
        }

        public void ContinueReading() => _readPauseEvent.Set();

        public bool IsProcessingNotOver() => _processThreads.Any(th => th?.IsAlive ?? false);

        public void PauseWriting()
        {
            _writePauseEvent.Reset();
            _writePauseEvent.WaitOne();
        }

        public void ContinueWriting() => _writePauseEvent.Set();

        public bool IsUsedMemoryMoreLimit()
        {
            _currentProcess.Refresh();
            return _currentProcess.WorkingSet64 > _memoryLimit;
        }

        private long GetMemoryLimit()
        {
            long memoryLimit;
            var halfAvailableMemory = (long)(new ComputerInfo().AvailablePhysicalMemory / 2);
            var memoryLimitFor32BitProcess = (long)(1.4 * 1024 * 1024 * 1024);

            if (!Environment.Is64BitProcess)
            {
                memoryLimit = memoryLimitFor32BitProcess < halfAvailableMemory
                                ? memoryLimitFor32BitProcess
                                : halfAvailableMemory;
            }
            else
            {
                memoryLimit = halfAvailableMemory;
            }

            return memoryLimit;
        }
    }
}
