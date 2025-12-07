using System.Collections.ObjectModel;
using System.Diagnostics;
using Hexa.NET.ImGui;

namespace CentrED.UI;

public class MultiSelectStorage<T>
{
    private List<T> _input = [];
    private HashSet<T> _selected = [];
    
    public void Begin(List<T> input)
    {
        _input = input;
    }
    
    public void HandleRequests(ImGuiMultiSelectIOPtr msIo)
    {
        Debug.Assert(msIo.ItemsCount != -1, "Missing value for items_count in BeginMultiSelect() call!");
        Debug.Assert(msIo.ItemsCount == _input.Count, "Items count mismatched BeginMultiSelect() vs this.Begin()");
        for (var i = 0; i < msIo.Requests.Size; i++)
        {
            var req = msIo.Requests[i];
            if (req.Type == ImGuiSelectionRequestType.SetAll)
            {
                _selected.Clear();
                if (req.Selected == 1)
                {
                    _selected = _input.ToHashSet();
                }
            }
            else if(req.Type == ImGuiSelectionRequestType.SetRange)
            {
                for (var j = req.RangeFirstItem; j <= req.RangeLastItem; j++)
                {
                    if (req.Selected == 1)
                    {
                        _selected.Add(_input[(int)j]);
                    }
                    else
                    {
                        _selected.Remove(_input[(int)j]);
                    }
                }
            }
        }
        
    }

    public bool Contains(T n)
    {
        return _selected.Contains(n);
    }

    public int Size => _selected.Count;

    public ReadOnlySet<T> Items => _selected.AsReadOnly();
}