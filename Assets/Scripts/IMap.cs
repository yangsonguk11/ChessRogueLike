using System.Collections.Generic;

public interface IMap
{
    IReadOnlyList<NodeRow> MapData { get; }
    int TotalFloors { get; }
}
