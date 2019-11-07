using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace WizdomClientStd
{
    public class AccessDeniedException: Exception
    {
        public AccessDeniedException() : base() { }
        public AccessDeniedException(string message) : base (message) { }
        public AccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
        protected AccessDeniedException(SerializationInfo info, StreamingContext context) : base (info, context) { }
    }

    public class ServerException : Exception
    {
        public string ServerResponse { get; set; }
        public string ServerHeaders { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public ServerException() : base() { }
        public ServerException(string message) : base(message) { }
        public ServerException(string message, string serverResponse, HttpStatusCode statusCode, string serverHeaders) : base(message)
        {
            ServerResponse = serverResponse;
            StatusCode = statusCode;
            ServerHeaders = serverHeaders;
        }
        public ServerException(string message, Exception innerException) : base(message, innerException) { }
        protected ServerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
