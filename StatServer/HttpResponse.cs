namespace StatServer
{
    public class HttpResponse
    {
        public readonly int Code;
        public readonly string Message;

        public HttpResponse(int code)
        {
            Code = code;
        }

        public HttpResponse(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
