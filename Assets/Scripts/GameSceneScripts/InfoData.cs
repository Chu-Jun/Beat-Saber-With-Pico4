using System.Collections.Generic;

[System.Serializable]
public class InfoData
{
    public string _version;
    public string _songName;
    public string _songSubName;
    public string _songAuthorName;
    public string _levelAuthorName;
    public float _beatsPerMinute;
    public string _songFilename;
    public string _coverImageFilename;
    public List<DifficultyBeatmapSet> _difficultyBeatmapSets;
}

[System.Serializable]
public class DifficultyBeatmapSet
{
    public string _beatmapCharacteristicName;
    public List<DifficultyBeatmap> _difficultyBeatmaps;
}

[System.Serializable]
public class DifficultyBeatmap
{
    public string _difficulty;
    public int _difficultyRank;
    public string _beatmapFilename;
    public float _noteJumpMovementSpeed;
    public float _noteJumpStartBeatOffset;
}