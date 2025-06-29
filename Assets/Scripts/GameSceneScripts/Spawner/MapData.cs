using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public string _version;
    public List<BPMEvent> _events;
    public List<Note> _notes;
    public List<Obstacle> _obstacles;
    public List<Waypoint> _waypoints;
    public List<SpecialEventGroup> _specialEvents;

    [System.Serializable]
    public class BPMEvent
    {
        public float _time;
        public float _bpm;
    }

    [System.Serializable]
    public class Note
    {
        public float _time;
        public int _lineIndex;
        public int _lineLayer;
        public int _type;
        public int _cutDirection;
    }

    [System.Serializable]
    public class Obstacle
    {
        public float _time;
        public int _lineIndex;
        public int _type;
        public float _duration;
        public int _width;
    }

    [System.Serializable]
    public class Waypoint
    {
        public float _time;
        public int _lineIndex;
        public int _lineLayer;
        public int _offsetDirection;
    }

    [System.Serializable]
    public class SpecialEventGroup
    {
        public float _time;
        public int _groupId;
        public List<SpecialEvent> _data;
    }

    [System.Serializable]
    public class SpecialEvent
    {
        public int _type;
        public int _value;
        public float _floatValue;
    }
}