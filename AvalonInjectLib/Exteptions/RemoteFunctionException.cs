namespace AvalonInjectLib.Exteptions
{
    public class RemoteFunctionException : Exception
    {
        public string Operation { get; }
        public int? ErrorCode { get; }

        public RemoteFunctionException(string operation, string message) : base(message)
        {
            Operation = operation;
        }

        public RemoteFunctionException(string operation, string message, int errorCode) : base(message)
        {
            Operation = operation;
            ErrorCode = errorCode;
        }

        public RemoteFunctionException(string operation, string message, Exception innerException) : base(message, innerException)
        {
            Operation = operation;
        }
    }
}
