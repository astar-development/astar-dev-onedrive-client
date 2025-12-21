using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions.Messages;

public static partial class AStarLog
{
    /// <summary>
    /// Provides static methods and utilities for web-related operations.
    /// </summary>
    /// <remarks>This class serves as a central entry point for web functionality. All members are static and
    /// can be accessed without instantiating the class.</remarks>
    public static partial class Web
    {
        /// <summary>
        /// Logs a warning for a Bad Request (400) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that caused the bad request.</param>
        [LoggerMessage(EventId = 400, Level = LogLevel.Warning, Message = "Bad Request (400) for `{Path}`")]
        public static partial void BadRequest(ILogger logger, string path);

        /// <summary>
        /// Logs a warning for an Unauthorized (401) request at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that was unauthorized.</param>
        [LoggerMessage(EventId = 401, Level = LogLevel.Warning, Message = "Unauthorized (401) for `{Path}`")]
        public static partial void Unauthorized(ILogger logger, string path);

        /// <summary>
        /// Logs a warning for a Forbidden (403) request at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that was forbidden.</param>
        [LoggerMessage(EventId = 403, Level = LogLevel.Warning, Message = "Forbidden (403) for `{Path}`")]
        public static partial void Forbidden(ILogger logger, string path);

        /// <summary>
        /// Logs a warning for a Not Found (404) request at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that was not found.</param>
        [LoggerMessage(EventId = 404, Level = LogLevel.Warning, Message = "Not Found (404) for `{Path}`")]
        public static partial void NotFound(ILogger logger, string path);

        /// <summary>
        /// Logs a warning for a Conflict (409) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that caused the conflict.</param>
        [LoggerMessage(EventId = 409, Level = LogLevel.Warning, Message = "Conflict (409) for `{Path}`")]
        public static partial void Conflict(ILogger logger, string path);

        /// <summary>
        /// Logs a warning for an Unprocessable Entity (422) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that was unprocessable.</param>
        [LoggerMessage(EventId = 422, Level = LogLevel.Warning, Message = "Unprocessable Entity (422) for `{Path}`")]
        public static partial void UnprocessableEntity(ILogger logger, string path);

        /// <summary>
        /// Logs a warning for Too Many Requests (429) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that received too many requests.</param>
        [LoggerMessage(EventId = 429, Level = LogLevel.Warning, Message = "Too Many Requests (429) for `{Path}`")]
        public static partial void TooManyRequests(ILogger logger, string path);

        /// <summary>
        /// Logs an error for an Internal Server Error (500) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that caused the internal server error.</param>
        [LoggerMessage(EventId = 500, Level = LogLevel.Error, Message = "Internal Server Error (500) for `{Path}`")]
        public static partial void InternalServerError(ILogger logger, string path);

        /// <summary>
        /// Logs an error for a Bad Gateway (502) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that caused the bad gateway error.</param>
        [LoggerMessage(EventId = 502, Level = LogLevel.Error, Message = "Bad Gateway (502) for `{Path}`")]
        public static partial void BadGateway(ILogger logger, string path);

        /// <summary>
        /// Logs an error for a Service Unavailable (503) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that was unavailable.</param>
        [LoggerMessage(EventId = 503, Level = LogLevel.Error, Message = "Service Unavailable (503) for `{Path}`")]
        public static partial void ServiceUnavailable(ILogger logger, string path);

        /// <summary>
        /// Logs an error for a Gateway Timeout (504) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="path">The request path that caused the gateway timeout.</param>
        [LoggerMessage(EventId = 504, Level = LogLevel.Error, Message = "Gateway Timeout (504) for `{Path}`")]
        public static partial void GatewayTimeout(ILogger logger, string path);
    }
}
