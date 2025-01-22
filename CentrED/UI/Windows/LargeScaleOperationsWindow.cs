using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class LSOWindow : Window
{
    public override string Name => "Large Scale Operations";

    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    private int x1;
    private int y1;
    private int x2;
    private int y2;
    private int mode;

    private int copyMove_type = 0;
    private int copyMove_offsetX = 0;
    private int copyMove_offsetY = 0;
    private bool copyMove_erase = false;

    private int setAltitude_type = 1;
    private int setAltitude_minZ = -128;
    private int setAltitude_maxZ = 127;
    private int setAltitude_relativeZ = 0;

    private string drawLand_idsText = "";

    private string deleteStatics_idsText = "";
    private int deleteStatics_minZ = -128;
    private int deleteStatics_maxZ = 127;

    private string addStatics_idsText = "";
    private int addStatics_chance = 100;
    private int addStatics_type = 1;
    private int addStatics_fixedZ = 0;

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
        if (ImGui.Button(minimapWindow.Show ? "Close Minimap" : "Open Minimap"))
        {
            minimapWindow.Show = !minimapWindow.Show;
        }
        ImGui.Separator();
        ImGui.Text("Area");
        ImGui.PushItemWidth(90);
        ImGui.InputInt("X1", ref x1);
        ImGui.SameLine();
        ImGui.InputInt("Y1", ref y1);
        ImGui.SameLine();
        if (ImGui.Button("Current pos##pos1"))
        {
            var pos = CEDGame.MapManager.TilePosition;
            x1 = pos.X;
            y1 = pos.Y;
        }
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos1"))
        {
            var tile = Application.CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x1 = tile.Tile.X;
                y1 = tile.Tile.Y;
            }
        }
        ImGui.InputInt("X2", ref x2);
        ImGui.SameLine();
        ImGui.InputInt("Y2", ref y2);
        ImGui.SameLine();
        if (ImGui.Button("Current pos##pos2"))
        {
            var pos = CEDGame.MapManager.TilePosition;
            x2 = pos.X;
            y2 = pos.Y;
        }
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos2"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x2 = tile.Tile.X;
                y2 = tile.Tile.Y;
            }
        }
        ImGui.PopItemWidth();
        ImGui.Separator();
        ImGui.RadioButton("Copy/Move", ref mode, 0);
        ImGui.RadioButton("Elevate", ref mode, 1);
        ImGui.RadioButton("Land", ref mode, 2);
        ImGui.RadioButton("Remove Statics", ref mode, 3);
        ImGui.RadioButton("Add Statics", ref mode, 4);
        ImGui.Separator();
        ImGui.Text("Parameters");
        switch (mode)
        {
            case 0:
            {
                ImGui.Text("Operation Type");
                ImGui.RadioButton("Copy", ref copyMove_type, (int)LSO.CopyMove.Copy);
                ImGui.SameLine();
                ImGui.RadioButton("Move", ref copyMove_type, (int)LSO.CopyMove.Move);
                UIManager.DragInt("Offset X", ref copyMove_offsetX, 1, -CEDClient.Width * 8, CEDClient.Width * 8);
                UIManager.DragInt("Offset Y", ref copyMove_offsetY, 1, -CEDClient.Height * 8, CEDClient.Height * 8);
                ImGui.Checkbox("Erase statics from target area", ref copyMove_erase);
                break;
            }
            case 1:
            {
                ImGui.Text("Operation Type");
                ImGui.RadioButton("Terrain", ref setAltitude_type, (int)LSO.SetAltitude.Terrain);
                UIManager.Tooltip("Set terrain altitude\n" +
                                  "Terrain altitude will be changed to a random value between minZ and maxZ\n" +
                                  "Statics will be elevated according to the terrain change");
                ImGui.SameLine();
                ImGui.RadioButton("Relative", ref setAltitude_type, (int)LSO.SetAltitude.Relative);
                UIManager.Tooltip("Relative altitude change\n" + 
                                  "Terrain and statics altitude will be changed by the specified amount");
                if (setAltitude_type == (int)LSO.SetAltitude.Terrain)
                {
                    UIManager.DragInt("MinZ", ref setAltitude_minZ, 1, -128, 127);
                    UIManager.DragInt("MaxZ", ref setAltitude_maxZ, 1, -128, 127);
                }
                else
                {
                    UIManager.DragInt("RelatizeZ", ref setAltitude_relativeZ, 1, -128, 127);
                }
                break;
        }
            case 2:
            {
                ImGui.InputText("ids", ref drawLand_idsText, 1024);
                break;
            }
            case 3:
            {
                ImGui.InputText("ids", ref deleteStatics_idsText, 1024);
                UIManager.Tooltip("Leave empty to remove all statics");
                UIManager.DragInt("MinZ", ref deleteStatics_minZ, 1, -128, 127);
                UIManager.DragInt("MaxZ", ref deleteStatics_maxZ, 1, -128, 127);
                break;
            }
            case 4:
            {
                ImGui.InputText("ids", ref addStatics_idsText, 1024);
                ImGui.DragInt("Chance", ref addStatics_chance, 1, 0, 100);
                ImGui.Text("Placement type");
                ImGui.RadioButton("Terrain", ref addStatics_type, (int)LSO.StaticsPlacement.Terrain);
                ImGui.RadioButton("On Top", ref addStatics_type, (int)LSO.StaticsPlacement.Top);
                ImGui.RadioButton("Fixed Z", ref addStatics_type, (int)LSO.StaticsPlacement.Fix);
                if (addStatics_type == (int)LSO.StaticsPlacement.Fix)
                {
                    UIManager.DragInt("Z", ref addStatics_fixedZ, 1, -128, 127);
                }
                break;
            }
            default:
            {
                ImGui.Text("How did you get here?");
                break;
            }
        }
        if (ImGui.Button("Submit"))
        {
            ILargeScaleOperation? lso = mode switch
            {
                0 => new LSOCopyMove((LSO.CopyMove)copyMove_type, copyMove_erase, copyMove_offsetX, copyMove_offsetY),
                1 => setAltitude_type switch
                {
                    (int)LSO.SetAltitude.Terrain => new LSOSetAltitude((sbyte)setAltitude_minZ, (sbyte)setAltitude_maxZ),
                    (int)LSO.SetAltitude.Relative => new LSOSetAltitude((sbyte)setAltitude_relativeZ),
                    _ => null
                },
                2 => new LSODrawLand(drawLand_idsText.Split(',').Select(ushort.Parse).ToArray()),
                3 => new LSODeleteStatics(deleteStatics_idsText, (sbyte)deleteStatics_minZ, (sbyte)deleteStatics_maxZ),
                4 => new LSOAddStatics(addStatics_idsText.Split(',').Select(s => (ushort)(int.Parse(s) + 0x4000)).ToArray(), (byte)addStatics_chance, (LSO.StaticsPlacement)addStatics_type, (sbyte)addStatics_fixedZ),
                _ => null
            };
            
            if(lso != null)
                CEDClient.Send(new LargeScaleOperationPacket([new AreaInfo((ushort)x1,(ushort)y1,(ushort)x2,(ushort)y2)], lso));
        }
    }

    public void DrawArea(System.Numerics.Vector2 currentPos)
    {
        if (!Show)
            return;
        if (x1 != 0 || y1 != 0 || x2 != 0 || y2 != 0)
        {
            ImGui.GetWindowDrawList().AddRect(
                currentPos + new System.Numerics.Vector2(x1 / 8, y1 / 8), 
                currentPos + new System.Numerics.Vector2(x2 / 8, y2 / 8), 
                ImGui.GetColorU32(UIManager.Green)
            );
        }
    }
}