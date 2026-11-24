namespace EarthRelief3D.Core.Help;

/// <summary>A link under a panel section: the document's short description and its address (in Core since the help catalog builds links too).</summary>
public sealed record PanelLink(string Text, Uri Url);
