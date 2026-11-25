namespace Wortlaut.Api.Elasticsearch;

public sealed class SpeechIndexException(string message, Exception? innerException = null)
    : Exception(message, innerException);
