using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;
using static CentrED.Application;

namespace CentrED;

// This class is almost exact copy from ClassicUO
// The only difference is that it works on GameTime passed to Process() and animated fields option is removed
sealed class AnimatedStaticsManager
    {
        private readonly FastList<StaticAnimationInfo> _staticInfos = new ();
        private uint _processTime;


        public unsafe void Initialize()
        {
            UOFile file = CEDGame.MapManager.UoFileManager.AnimData.AnimDataFile;

            if (file == null)
            {
                return;
            }

            uint lastaddr = (uint)(file.Length - sizeof(AnimDataFrame));

            for (int i = 0; i < CEDGame.MapManager.UoFileManager.TileData.StaticData.Length; i++)
            {
                if (CEDGame.MapManager.UoFileManager.TileData.StaticData[i].IsAnimated)
                {
                    uint addr = (uint)(i * 68 + 4 * (i / 8 + 1));

                    if (addr <= lastaddr)
                    {
                        _staticInfos.Add
                        (
                            new StaticAnimationInfo
                            {
                                Index = (ushort)i,
                                // IsField = StaticFilters.IsField((ushort)i)
                            }
                        );
                    }
                }
            }
        }

        public unsafe void Process(GameTime gameTime)
        {
            var ticks = (uint)gameTime.TotalGameTime.TotalMilliseconds;
            if (_staticInfos == null || _staticInfos.Length == 0 || _processTime >= ticks)
            {
                return;
            }

            var file = CEDGame.MapManager.UoFileManager.AnimData.AnimDataFile;

            if (file == null)
            {
                return;
            }
            
            // fix static animations time to reflect the standard client
            uint delay = 50 * 2;
            uint next_time = ticks + 250;
            // bool no_animated_field = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.FieldsType != 0;
            UOFileIndex[] static_data = CEDGame.MapManager.UoFileManager.Arts.File.Entries;

            for (int i = 0; i < _staticInfos.Length; i++)
            {
                ref StaticAnimationInfo o = ref _staticInfos.Buffer[i];

                // if (no_animated_field && o.IsField)
                // {
                //     o.AnimIndex = 0;
                //
                //     continue;
                // }

                if (o.Time < ticks)
                {
                    uint addr = (uint)(o.Index * 68 + 4 * (o.Index / 8 + 1));
                    file.Seek(addr, SeekOrigin.Begin);
                    var info = file.Read<AnimDataFrame>();

                    byte offset = o.AnimIndex;

                    if (info.FrameInterval > 0)
                    {
                        o.Time = ticks + info.FrameInterval * delay + 1;
                    }
                    else
                    {
                        o.Time = ticks + delay;
                    }

                    if (offset < info.FrameCount && o.Index + 0x4000 < static_data.Length)
                    {
                        static_data[o.Index + 0x4000].AnimOffset = info.FrameData[offset++];
                    }

                    if (offset >= info.FrameCount)
                    {
                        offset = 0;
                    }

                    o.AnimIndex = offset;
                }

                if (o.Time < next_time)
                {
                    next_time = o.Time;
                }
            }

            _processTime = next_time;
        }


        private struct StaticAnimationInfo
        {
            public uint Time;
            public ushort Index;
            public byte AnimIndex;
            // public bool IsField;
        }
    }