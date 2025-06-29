using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MapLoader : MonoBehaviour
{
    public class MapLoadResult
    {
        public InfoData InfoData { get; set; }
        public MapData MapData { get; set; }
        public AudioClip AudioClip { get; set; }
        public float BPM { get; set; }
        public float NoteJumpMovementSpeed { get; set; }
        public float NoteJumpStartBeatOffset { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    private MapLoadResult lastResult;

    public IEnumerator LoadMapAsync(string mapDirectoryPath, int difficultyLevel)
    {
        var result = new MapLoadResult();
        lastResult = result;

        // Validate input parameters
        if (string.IsNullOrEmpty(mapDirectoryPath))
        {
            result.Success = false;
            result.ErrorMessage = "Map directory path cannot be null or empty";
            yield break;
        }

        if (!Directory.Exists(mapDirectoryPath))
        {
            result.Success = false;
            result.ErrorMessage = $"Map directory does not exist: {mapDirectoryPath}";
            yield break;
        }

        if (difficultyLevel < 0)
        {
            result.Success = false;
            result.ErrorMessage = "Difficulty level cannot be negative";
            yield break;
        }

        yield return StartCoroutine(LoadInfoData(mapDirectoryPath, result));
        if (!result.Success) yield break;

        yield return StartCoroutine(LoadAudioClip(mapDirectoryPath, result));
        if (!result.Success) yield break;

        yield return StartCoroutine(LoadMapData(mapDirectoryPath, difficultyLevel, result));
    }

    public MapLoadResult GetLastResult()
    {
        return lastResult;
    }

    private IEnumerator LoadInfoData(string mapDirectoryPath, MapLoadResult result)
    {
        string infoPath = Path.Combine(mapDirectoryPath, "Info.dat");
        if (!File.Exists(infoPath))
        {
            result.Success = false;
            result.ErrorMessage = $"Info.dat not found in: {mapDirectoryPath}";
            yield break;
        }

        try
        {
            string infoJson = File.ReadAllText(infoPath);
            result.InfoData = JsonUtility.FromJson<InfoData>(infoJson);
            result.BPM = result.InfoData._beatsPerMinute;
            result.Success = true;
        }
        catch (System.Exception e)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse Info.dat: {e.Message}";
        }
    }

    private IEnumerator LoadAudioClip(string mapDirectoryPath, MapLoadResult result)
    {
        string songFileName = result.InfoData._songFilename;
        string oggFileName = Path.ChangeExtension(songFileName, ".ogg");
        string audioPath = Path.Combine(mapDirectoryPath, oggFileName);
        if (!File.Exists(audioPath))
        {
            result.Success = false;
            result.ErrorMessage = $"Audio file not found: {oggFileName}";
            yield break;
        }

        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + Path.GetFullPath(audioPath), AudioType.OGGVORBIS))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                result.AudioClip = DownloadHandlerAudioClip.GetContent(uwr);
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = $"Error loading audio: {uwr.error}";
            }
        }
    }

    private IEnumerator LoadMapData(string mapDirectoryPath, int difficultyLevel, MapLoadResult result)
    {
        // Validate difficulty data exists
        if (result.InfoData._difficultyBeatmapSets == null || 
            result.InfoData._difficultyBeatmapSets.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No difficulty beatmap sets found in Info.dat";
            yield break;
        }

        var beatmapSet = result.InfoData._difficultyBeatmapSets[0];
        if (beatmapSet._difficultyBeatmaps == null || 
            beatmapSet._difficultyBeatmaps.Count <= difficultyLevel)
        {
            result.Success = false;
            result.ErrorMessage = $"Difficulty level {difficultyLevel} not found (available: 0-{beatmapSet._difficultyBeatmaps.Count - 1})";
            yield break;
        }

        DifficultyBeatmap selectedBeatmap = beatmapSet._difficultyBeatmaps[difficultyLevel];
        result.NoteJumpMovementSpeed = selectedBeatmap._noteJumpMovementSpeed;
        result.NoteJumpStartBeatOffset = selectedBeatmap._noteJumpStartBeatOffset;

        string mapPath = Path.Combine(mapDirectoryPath, selectedBeatmap._beatmapFilename);
        if (!File.Exists(mapPath))
        {
            result.Success = false;
            result.ErrorMessage = $"Map file not found: {selectedBeatmap._beatmapFilename}";
            yield break;
        }

        try
        {
            string mapJson = File.ReadAllText(mapPath);
            mapJson = NormalizeMapJson(mapJson);
            result.MapData = JsonUtility.FromJson<MapData>(mapJson);

            // Validate parsed data
            if (result.MapData == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to parse map data: MapData is null";
                yield break;
            }

            result.Success = true;
        }
        catch (System.Exception e)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse map data: {e.Message}";
        }
    }

    private string NormalizeMapJson(string mapJson)
    {
        // Check if it's v3 format
        if (mapJson.Contains("\"colorNotes\":"))
        {
            // More robust replacement using regex or proper JSON parsing
            var replacements = new Dictionary<string, string>
            {
                { "\"colorNotes\":", "\"_notes\":" },
                { "\"b\":", "\"_time\":" },
                { "\"x\":", "\"_lineIndex\":" },
                { "\"y\":", "\"_lineLayer\":" },
                { "\"c\":", "\"_type\":" },
                { "\"d\":", "\"_cutDirection\":" }
            };

            foreach (var replacement in replacements)
            {
                mapJson = mapJson.Replace(replacement.Key, replacement.Value);
            }
        }
        return mapJson;
    }
}