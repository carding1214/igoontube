namespace PUPlayer.Core.Scenes;

public enum SceneMarkerKind { Voice, Detail, HighActivity, Favorite }
public sealed record SceneMarker(double Seconds, SceneMarkerKind Kind, string Label);
