using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Server.MirEnvir;

namespace Server.MirDatabase
{
    public class MapInfo
    {
        public int Index;
        public string FileName = string.Empty, Title = string.Empty;
        public ushort MiniMap, BigMap, Music;
        public LightSetting Light;
        public byte MapDarkLight = 0, MineIndex = 0;

        public bool NoTeleport, NoReconnect, NoRandom, NoEscape, NoRecall, NoDrug, NoPosition, NoFight, SafeZone,
            NoThrowItem, NoDropPlayer, NoDropMonster, NoNames, NoMount, NeedBridle, Fight, NeedHole, Fire, Lightning, NoHero, GT, NoGroup = false, NoRes;

        public string NoReconnectMap = string.Empty;
        public int FireDamage, LightningDamage;

        public List<SafeZoneInfo> SafeZones = new List<SafeZoneInfo>();
        public List<MovementInfo> Movements = new List<MovementInfo>();
        public List<RespawnInfo> Respawns = new List<RespawnInfo>();
        public List<NPCInfo> NPCs = new List<NPCInfo>();
        public List<MineZone> MineZones = new List<MineZone>();
        public List<Point> ActiveCoords = new List<Point>();
        public List<PublicEventInfo> PublicEvents = new List<PublicEventInfo>();
        public InstanceInfo Instance;
        public List<LMS_BR_Info> LMS_BR = new List<LMS_BR_Info>();
        public MapInfo()
        {

        }

        /// <summary>
        /// �� Map.db �м��ص�ͼ��Ϣ
        /// </summary>
        /// <param name="reader"></param>
        public MapInfo(BinaryReader reader)
        {
            //1�����ص�ͼ�α꣺1
            Index = reader.ReadInt32();
            //2�����ص�ͼ�ļ�����0��
            FileName = reader.ReadString();
            //3�����ص�ͼ���ƣ������족
            Title = reader.ReadString();
            //4������С��ͼ��ţ�1
            MiniMap = reader.ReadUInt16();
            //5�����ص�ͼ������0
            Light = (LightSetting) reader.ReadByte();

            //6�����ش��ͼ��3001
            if (Envir.LoadVersion >= 3) 
                BigMap = reader.ReadUInt16();

            //7�����ص�ͼ��ȫ��������7
            int count = reader.ReadInt32();
            //8�����ص�ͼ��ȫ����
            for (int i = 0; i < count; i++)
                SafeZones.Add(new SafeZoneInfo(reader) { Info = this });

            //9�����ع��������������278
            count = reader.ReadInt32();
            //10�����ع�������㣺�α�ָ��158
            for (int i = 0; i < count; i++)
                Respawns.Add(new RespawnInfo(reader, Envir.LoadVersion, Envir.LoadCustomVersion));

            if (Envir.LoadVersion <= 33)
            {
                //����NPC�������Ͱ汾��
                count = reader.ReadInt32();
                //����NPC���Ͱ汾��
                for (int i = 0; i < count; i++)
                    NPCs.Add(new NPCInfo(reader));
            }

            //11��·��������71���α�ָ��8533
            count = reader.ReadInt32();
            //12��·��
            for (int i = 0; i < count; i++)
                Movements.Add(new MovementInfo(reader));

            //���������� �汾14 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 14) 
                return;

            //13����ֹ����
            NoTeleport = reader.ReadBoolean();
            //14���Ƿ��ֹ����
            NoReconnect = reader.ReadBoolean();
            //15����ֹ������ͼ����
            NoReconnectMap = reader.ReadString();
            //16����ֹ���
            NoRandom = reader.ReadBoolean();
            //17���Ƿ��ֹ���ξ�
            NoEscape = reader.ReadBoolean();
            //18��
            NoRecall = reader.ReadBoolean();
            //19��
            NoDrug = reader.ReadBoolean();
            //20����ֹ̽��
            NoPosition = reader.ReadBoolean();
            //21���Ƿ��ֹ������Ʒ
            NoThrowItem = reader.ReadBoolean();
            //22���Ƿ��ֹ��ұ���Ʒ
            NoDropPlayer = reader.ReadBoolean();
            //23���Ƿ��ֹ���ﱬ��Ʒ
            NoDropMonster = reader.ReadBoolean();
            //24������ʾ����
            NoNames = reader.ReadBoolean();
            //26��
            Fight = reader.ReadBoolean();
            //�汾14ר��
            if (Envir.LoadVersion == 14) 
                NeedHole = reader.ReadBoolean();
            //27��
            Fire = reader.ReadBoolean();
            //28��
            FireDamage = reader.ReadInt32();
            //29��
            Lightning = reader.ReadBoolean();
            //30��
            LightningDamage = reader.ReadInt32();
            //���������� �汾23 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 23) 
                return;
            //31��
            MapDarkLight = reader.ReadByte();
            //���������� �汾26 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 26) 
                return;
            //32�����ؿ�������
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                //33�����ؿ�����һ��MineZone��11�ֽڣ�
                MineZones.Add(new MineZone(reader));
            //���������� �汾27 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 27) 
                return;
            //34�������α�
            MineIndex = reader.ReadByte();

            //���������� �汾33 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 33) 
                return;
            //35��
            NoMount = reader.ReadBoolean();
            //36��
            NeedBridle = reader.ReadBoolean();

            //���������� �汾42 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 42) 
                return;
            //37����ֹPK
            NoFight = reader.ReadBoolean();

            //���������� �汾53 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 53) 
                return;
            //38����ͼ����
            Music = reader.ReadUInt16();

            //���������� �汾103 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 103) 
                return;
            NoHero = reader.ReadBoolean();

            //���������� �汾110 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 110) 
                return;
            GT = reader.ReadBoolean();

            //���������� �汾117 �Ժ�׷�ӣ�
            if (Envir.LoadVersion < 117) 
                return;
            SafeZone = reader.ReadBoolean();



            //���������� �汾135 �Ժ�׷�ӣ�
            if (Envir.LoadVersion > 135)
            {
                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    PublicEvents.Add(new PublicEventInfo(reader));
            }
            //���������� �汾140 �Ժ�׷�ӣ�
            if (Envir.LoadVersion > 140)
                NoGroup = reader.ReadBoolean();
            //���������� �汾144 �Ժ�׷�ӣ�
            if (Envir.LoadVersion > 144)
                NoRes = reader.ReadBoolean();
            //���������� �汾145 �Ժ�׷�ӣ�
            if (Envir.LoadVersion > 145)
            {
                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    LMS_BR.Add(new LMS_BR_Info(reader, Envir.LoadVersion));
            }
        }

        /// <summary>
        /// �� Map.db �б����ͼ��Ϣ
        /// </summary>
        /// <param name="writer"></param>
        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(FileName);
            writer.Write(Title);
            writer.Write(MiniMap);
            writer.Write((byte)Light);
            writer.Write(BigMap);
            writer.Write(SafeZones.Count);
            for (int i = 0; i < SafeZones.Count; i++)
                SafeZones[i].Save(writer);

            writer.Write(Respawns.Count);
            for (int i = 0; i < Respawns.Count; i++)
                Respawns[i].Save(writer);

            writer.Write(Movements.Count);
            for (int i = 0; i < Movements.Count; i++)
                Movements[i].Save(writer);

            writer.Write(NoTeleport);
            writer.Write(NoReconnect);
            writer.Write(NoReconnectMap);
            writer.Write(NoRandom);
            writer.Write(NoEscape);
            writer.Write(NoRecall);
            writer.Write(NoDrug);
            writer.Write(NoPosition);
            writer.Write(NoThrowItem);
            writer.Write(NoDropPlayer);
            writer.Write(NoDropMonster);
            writer.Write(NoNames);
            writer.Write(Fight);
            writer.Write(Fire);
            writer.Write(FireDamage);
            writer.Write(Lightning);
            writer.Write(LightningDamage);
            writer.Write(MapDarkLight);
            writer.Write(MineZones.Count);
            for (int i = 0; i < MineZones.Count; i++)
                MineZones[i].Save(writer);
            writer.Write(MineIndex);

            writer.Write(NoMount);
            writer.Write(NeedBridle);

            writer.Write(NoFight);

            writer.Write(Music);

            writer.Write(NoHero);
            writer.Write(GT);
            writer.Write(SafeZone);




            writer.Write(PublicEvents.Count);
            for (int i = 0; i < PublicEvents.Count; i++)
                PublicEvents[i].Save(writer);
            writer.Write(NoGroup);
            writer.Write(NoRes);
            writer.Write(LMS_BR.Count);
            for (int i = 0; i < LMS_BR.Count; i++)
                LMS_BR[i].Save(writer);
        }

        public void CreateLMS_BR()
        {
            LMS_BR.Add(new LMS_BR_Info { Index = ++SMain.EditEnvir.LMS_BR_Index });
        }

        public void CreatePublicEvent()
        {
            PublicEvents.Add(new PublicEventInfo { Info = this, Index = ++SMain.EditEnvir.MapEventIndex });
        }

        /// <summary>
        /// ������Ƭ��ͼ��Ԫ����
        /// </summary>
        public void CreateMap()
        {
            for (int j = 0; j < SMain.Envir.NPCInfoList.Count; j++)
            {
                if (SMain.Envir.NPCInfoList[j].MapIndex != Index) 
                    continue;

                NPCs.Add(SMain.Envir.NPCInfoList[j]);
            }

            Map map = new Map(this);

            if (map.Info.FileName == "orc25")
            {

            }

            //���ص�ͼ��Ƭ����
            if (!map.Load()) 
                return;

            SMain.Envir.MapList.Add(map);

            if (Instance == null)
            {
                Instance = new InstanceInfo(this, map);
            }

            for (int i = 0; i < SafeZones.Count; i++)
                if (SafeZones[i].StartPoint)
                    SMain.Envir.StartPoints.Add(SafeZones[i]);
        }

        public void CreateInstance()
        {
            if (Instance.MapList.Count == 0) return;

            Map map = new Map(this);
            if (!map.Load()) return;

            SMain.Envir.MapList.Add(map);

            Instance.AddMap(map);
        }

        public void CreateSafeZone()
        {
            SafeZones.Add(new SafeZoneInfo { Info = this });
        }

        public void CreateRespawnInfo()
        {
            Respawns.Add(new RespawnInfo { RespawnIndex = ++SMain.EditEnvir.RespawnIndex });
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Index, Title);
        }

        public void CreateNPCInfo()
        {
            NPCs.Add(new NPCInfo());
        }

        public void CreateMovementInfo()
        {
            Movements.Add(new MovementInfo());
        }

        public static void FromText(string text)
        {
            string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 8) return;

            MapInfo info = new MapInfo {FileName = data[0], Title = data[1]};


            if (!ushort.TryParse(data[2], out info.MiniMap)) return;

            if (!Enum.TryParse(data[3], out info.Light)) return;
            if (!int.TryParse(data[4], out int sziCount)) return;
            if (!int.TryParse(data[5], out int miCount)) return;
            if (!int.TryParse(data[6], out int riCount)) return;
            if (!int.TryParse(data[7], out int npcCount)) return;


            int start = 8;

            for (int i = 0; i < sziCount; i++)
            {
                SafeZoneInfo temp = new SafeZoneInfo { Info = info };
                if (!int.TryParse(data[start + (i * 4)], out int x)) return;
                if (!int.TryParse(data[start + 1 + (i * 4)], out int y)) return;
                if (!ushort.TryParse(data[start + 2 + (i * 4)], out temp.Size)) return;
                if (!bool.TryParse(data[start + 3 + (i * 4)], out temp.StartPoint)) return;

                temp.Location = new Point(x, y);
                info.SafeZones.Add(temp);
            }
            start += sziCount * 4;



            for (int i = 0; i < miCount; i++)
            {
                MovementInfo temp = new MovementInfo();
                if (!int.TryParse(data[start + (i * 5)], out int x)) return;
                if (!int.TryParse(data[start + 1 + (i * 5)], out int y)) return;
                temp.Source = new Point(x, y);

                if (!int.TryParse(data[start + 2 + (i * 5)], out temp.MapIndex)) return;

                if (!int.TryParse(data[start + 3 + (i * 5)], out x)) return;
                if (!int.TryParse(data[start + 4 + (i * 5)], out y)) return;
                temp.Destination = new Point(x, y);

                info.Movements.Add(temp);
            }
            start += miCount * 5;


            for (int i = 0; i < riCount; i++)
            {
                RespawnInfo temp = new RespawnInfo();
                if (!int.TryParse(data[start + (i * 7)], out temp.MonsterIndex)) return;
                if (!int.TryParse(data[start + 1 + (i * 7)], out int x)) return;
                if (!int.TryParse(data[start + 2 + (i * 7)], out int y)) return;

                temp.Location = new Point(x, y);

                if (!ushort.TryParse(data[start + 3 + (i * 7)], out temp.Count)) return;
                if (!ushort.TryParse(data[start + 4 + (i * 7)], out temp.Spread)) return;
                if (!ushort.TryParse(data[start + 5 + (i * 7)], out temp.Delay)) return;
                if (!byte.TryParse(data[start + 6 + (i * 7)], out temp.Direction)) return;
                if (!int.TryParse(data[start + 7 + (i * 7)], out temp.RespawnIndex)) return;
                if (!bool.TryParse(data[start + 8 + (i * 7)], out temp.SaveRespawnTime)) return;
                if (!ushort.TryParse(data[start + 9 + (i * 7)], out temp.RespawnTicks)) return;

                info.Respawns.Add(temp);
            }
            start += riCount * 7;


            for (int i = 0; i < npcCount; i++)
            {
                NPCInfo temp = new NPCInfo { FileName = data[start + (i * 6)], Name = data[start + 1 + (i * 6)] };
                if (!int.TryParse(data[start + 2 + (i * 6)], out int x)) return;
                if (!int.TryParse(data[start + 3 + (i * 6)], out int y)) return;

                temp.Location = new Point(x, y);

                if (!ushort.TryParse(data[start + 4 + (i * 6)], out temp.Rate)) return;
                if (!ushort.TryParse(data[start + 5 + (i * 6)], out temp.Image)) return;

                info.NPCs.Add(temp);
            }



            info.Index = ++SMain.EditEnvir.MapIndex;
            SMain.EditEnvir.MapInfoList.Add(info);
        }
    }

    public class InstanceInfo
    {
        //Constants
        public int PlayerCap = 2;
        public int MaxInstanceCount = 10;

        //
        public MapInfo MapInfo;
        public List<Map> MapList = new List<Map>();

        /*
         Notes
         Create new instance from here if all current maps are full
         Destroy maps when instance is empty - process loop in map or here?
         Change NPC INSTANCEMOVE to move and create next available instance

        */

        public InstanceInfo(MapInfo mapInfo, Map map)
        {
            MapInfo = mapInfo;
            AddMap(map);
        }

        public void AddMap(Map map)
        {
            MapList.Add(map);
        }

        public void RemoveMap(Map map)
        {
            MapList.Remove(map);
        }

        public Map GetFirstAvailableInstance()
        {
            for (int i = 0; i < MapList.Count; i++)
            {
                Map m = MapList[i];

                if (m.Players.Count < PlayerCap) return m;
            }

            return null;
        }

        public void CreateNewInstance()
        {
            MapInfo.CreateInstance();
        }
    }
}