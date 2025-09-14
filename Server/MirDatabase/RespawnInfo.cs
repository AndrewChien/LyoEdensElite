using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Server.MirEnvir;

namespace Server.MirDatabase
{
    /// <summary>
    /// 怪物的刷新数据包括从 DB 中读取的怪物刷新配置 RespawnInfo 以及基于它创建的刷怪点对象 MapRespawn
    /// 每个 Info 中只存储了一个 MonsterIndex，因此不同的怪物刷新需要独立的 RespawnInfo，
    /// RoutePath 为刷怪点的坐标配置文件路径，文件中通过逗号+换行分隔的字符串表示一系列坐标，每行一个刷怪点坐标。
    /// 
    /// 这里的 Delay 和 RespawnTicks 分别代表了两种怪物刷新系统，当没有配置 RespawnTicks 的时候 Delay 生效，
    /// 否则 RespawnTicks 生效，这个后面我们会更加详细的进行分析。
    /// 
    /// 上述刷怪点配置对象会在地图加载 Cell 完成后作为入参创建地图上的实际刷怪点控制对象 MapRespawn，
    /// 会完成从 RoutePath 读取坐标、绑定地图、绑定怪物等逻辑
    /// </summary>
    public class RespawnInfo
    {
        //这里的 Count, RespawnTime, NextSpawnTick 和 ErrorCount 将作为该刷怪点的运行时状态用于控制刷怪逻辑。
        public int MonsterIndex;
        public Point Location;
        public ushort Count, Spread, Delay, RandomDelay;
        public byte Direction;

        public string RoutePath = string.Empty;
        public int RespawnIndex;
        public bool SaveRespawnTime = false;
        public ushort RespawnTicks; //leave 0 if not using this system!

        public RespawnInfo()
        {

        }
        public RespawnInfo(BinaryReader reader, int Version, int Customversion)
        {
            MonsterIndex = reader.ReadInt32();

            Location = new Point(reader.ReadInt32(), reader.ReadInt32());

            Count = reader.ReadUInt16();
            Spread = reader.ReadUInt16();

            Delay = reader.ReadUInt16();
            Direction = reader.ReadByte();

            if (Envir.LoadVersion >= 36)
            {
                RoutePath = reader.ReadString();
            }

            if (Version > 67)
            {
                RandomDelay = reader.ReadUInt16();
                RespawnIndex = reader.ReadInt32();
                SaveRespawnTime = reader.ReadBoolean();
                RespawnTicks = reader.ReadUInt16();
            }
            else
            {
                RespawnIndex = ++SMain.Envir.RespawnIndex;
            }
        }

        public static RespawnInfo FromText(string text)
        {
            string[] data = text.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 7) return null;

            RespawnInfo info = new RespawnInfo();
            if (!int.TryParse(data[0], out info.MonsterIndex)) return null;
            if (!int.TryParse(data[1], out int x)) return null;
            if (!int.TryParse(data[2], out int y)) return null;

            info.Location = new Point(x, y);

            if (!ushort.TryParse(data[3], out info.Count)) return null;
            if (!ushort.TryParse(data[4], out info.Spread)) return null;
            if (!ushort.TryParse(data[5], out info.Delay)) return null;
            if (!byte.TryParse(data[6], out info.Direction)) return null;
            if (!ushort.TryParse(data[7], out info.RandomDelay)) return null;
            if (!int.TryParse(data[8], out info.RespawnIndex)) return null;
            if (!bool.TryParse(data[9], out info.SaveRespawnTime)) return null;
            if (!ushort.TryParse(data[10], out info.RespawnTicks)) return null;

            return info;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(MonsterIndex);

            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Count);
            writer.Write(Spread);

            writer.Write(Delay);
            writer.Write(Direction);

            writer.Write(RoutePath);

            writer.Write(RandomDelay);
            writer.Write(RespawnIndex);
            writer.Write(SaveRespawnTime);
            writer.Write(RespawnTicks);
            /*
            File.AppendAllText(Envir.exportInfo + @".\MapInfo\SafeZoneInfo\SAVE_SafeZoneInfo_.txt", string.Format("MonsterIndex {4}\nLocation X {0} Y {1}\nSpread = {2}\nCount {3}\nDelay {5}\nDirection {6}\nReoute Path {7}\nRandom Delay {8}\nRespawnIndex {9}\nSaveRespawnTime {10}\nRespawn Ticks {11}\n\n\n\n",
                    Location.X,
                    Location.Y,
                    Spread,
                    Count,
                    MonsterIndex, Delay, Direction, RoutePath, RandomDelay,
                    RespawnIndex, SaveRespawnTime, RespawnTicks));
                    */
        }

        public override string ToString()
        {
            return string.Format("Monster: {0} - {1} - {2} - {3} - {4} - {5} - {6} - {7} - {8} - {9}", MonsterIndex, Functions.PointToString(Location), Count, Spread, Delay, Direction, RandomDelay, RespawnIndex, SaveRespawnTime, RespawnTicks);
        }
    }

    public class RouteInfo
    {
        public Point Location;
        public int Delay;

        public static RouteInfo FromText(string text)
        {
            string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 2) return null;

            RouteInfo info = new RouteInfo();
            if (!int.TryParse(data[0], out int x)) return null;
            if (!int.TryParse(data[1], out int y)) return null;

            info.Location = new Point(x, y);

            if (data.Length <= 2) return info;

            return !int.TryParse(data[2], out info.Delay) ? info : info;
        }
    }
}