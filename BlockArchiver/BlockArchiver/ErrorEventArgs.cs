using System;

namespace BlockArchiver
{
    public class ErrorEventArgs
    {
        public Exception Exception { get; set; }
        public string Message { get; set; }

        public ErrorEventArgs(string message, Exception ex)
        {
            Message = message;
            Exception = ex;
        }
    }
}
