using System.Collections.Generic;

namespace StatServer
{
    public class HttpResponse
    {
        public enum Status
        {
            OK = 200,
            BadRequest = 400,
            NotFound = 404,
            MethodNotAllowed = 405
        }

        public static Status StringToStatus(string status)
        {
            var answers = new Dictionary<string, Status>
            {
                ["Not Found"] = Status.NotFound,
                //[""]
            };
            return answers[status];
        }

        public readonly int Code;
        public readonly string Message;

        public HttpResponse(Status status)
        {
            Code = (int)status;
        }

        public HttpResponse(Status status, string message)
        {
            Code = (int)status;
            Message = message;
        }
    }
}
