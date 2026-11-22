namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// A page, file or JSON document no longer looks the way the feed expects (a link, resource or column is gone). The message
/// always starts "format changed:" so the status line can say so. A feed throws it before it touches the store, so the
/// previous data stays.
/// </summary>
public sealed class FeedFormatException(string what) : Exception("format changed: " + what);
