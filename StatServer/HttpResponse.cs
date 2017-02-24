namespace StatServer
{
    public class HttpResponse
    {
        public enum Answer
        {
            OK = 200,
            BadRequest = 400,
            NotFound = 404,
            MethodNotAllowed = 405
        }

        public readonly int Code;
        public readonly string Message;

        public HttpResponse(Answer answer)
        {
            Code = (int)answer;
        }

        public HttpResponse(Answer answer, string message)
        {
            Code = (int)answer;
            Message = message;
        }
    }
}
