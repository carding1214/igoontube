namespace PUPlayer.App.AudioProcessing;

public enum AudioProcessingStage { Waiting, Separating, Encoding, Cached, Completed }

public sealed record AudioProcessingProgress(AudioProcessingStage Stage, string Message);
