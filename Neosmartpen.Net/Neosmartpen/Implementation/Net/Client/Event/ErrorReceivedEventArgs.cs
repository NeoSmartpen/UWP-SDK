namespace Neosmartpen.Net
{
    public enum ErrorType
    {
        MissingPenUp = 1,
        MissingPenDown = 2,
        InvalidTime = 3,
        MissingPenDownPenMove = 4,
        FilteredCode = 5,
        NdacError = 6
    }

    public sealed class ErrorReceivedEventArgs
    {
        internal ErrorReceivedEventArgs(ErrorType errorType, long ts)
        {
            ErrorType = errorType;
            Timestamp = ts;
        }

        internal ErrorReceivedEventArgs(ErrorType errorType, Dot dot, long ts)
        {
            ErrorType = errorType;
            Dot = dot;
            Timestamp = ts;
        }

        public ErrorType ErrorType { get; internal set; }

        public Dot Dot { get; internal set; }

        public long Timestamp { get; internal set; }
    }
}
