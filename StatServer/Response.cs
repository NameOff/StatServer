namespace StatServer
{
    public class Response
    {
        public enum Status
        {
            OK = 200,
            BadRequest = 400,
            NotFound = 404,
            MethodNotAllowed = 405
        }

        public readonly int Code;
        public readonly string Message;

        public Response(Status status)
        {
            Code = (int)status;
        }

        public Response(Status status, string message)
        {
            Code = (int)status;
            Message = message;
        }
    }
}
