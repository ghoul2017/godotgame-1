using System.Collections.Generic;

namespace GodotGame;

public sealed class SurfaceSelectionState
{
    private readonly List<string> _selectedUnitInstanceIds = new();

    public IReadOnlyList<string> SelectedUnitInstanceIds => _selectedUnitInstanceIds;

    public void Clear()
    {
        _selectedUnitInstanceIds.Clear();
    }

    public void SetSingle(string unitInstanceId)
    {
        _selectedUnitInstanceIds.Clear();
        Add(unitInstanceId);
    }

    public void Add(string unitInstanceId)
    {
        if (!_selectedUnitInstanceIds.Contains(unitInstanceId))
        {
            _selectedUnitInstanceIds.Add(unitInstanceId);
        }
    }

    public void SetMany(IEnumerable<string> unitInstanceIds)
    {
        _selectedUnitInstanceIds.Clear();
        foreach (string unitInstanceId in unitInstanceIds)
        {
            Add(unitInstanceId);
        }
    }

    public bool Contains(string unitInstanceId)
    {
        return _selectedUnitInstanceIds.Contains(unitInstanceId);
    }
}
