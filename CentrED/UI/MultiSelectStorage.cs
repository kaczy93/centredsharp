using System.Collections.ObjectModel;
using System.Diagnostics;
using Hexa.NET.ImGui;

namespace CentrED.UI;

public class MultiSelectStorage<T>(HashSet<T> initialState)
{
    private List<T> _input = [];
    private HashSet<T> _selected = initialState;
    
    public void Begin(List<T> input, ImGuiListClipperPtr clipper, ImGuiMultiSelectFlags extraFlags = ImGuiMultiSelectFlags.None)
    {
        _input = input;
        var flags = ImGuiMultiSelectFlags.NoSelectAll | extraFlags;
        var msIo = ImGui.BeginMultiSelect(flags, _selected.Count, input.Count);
        HandleRequests(msIo);
        if(msIo.RangeSrcItem != -1)
            clipper.IncludeItemByIndex((int)msIo.RangeSrcItem);
    }

    public void End()
    {
        var msIo = ImGui.EndMultiSelect();
        HandleRequests(msIo);
    }
    
    private void HandleRequests(ImGuiMultiSelectIOPtr msIo)
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

    public void SetSelection(T item)
    {
        _selected.Clear();
        _selected.Add(item);
    }

    public bool Contains(T n)
    {
        return _selected.Contains(n);
    }

    public ReadOnlySet<T> Items => _selected.AsReadOnly();
}