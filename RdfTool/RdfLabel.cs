﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace RdfTool
{
    public class RdfLabel
    {
        public FoxHash LabelName { get; set; }
        public byte u00 { get; set; } //max 0x0F 15
        public byte NoiseType { get; set; } //todo bits max 0x0F 15
        public byte u10 { get; set; } //max 0x0F 15
        public byte u11 { get; set; } //max 0x0F 15
        // voice clip count (arraysize) //max 0x3F 63
        public byte u21 { get; set; } //max 0x03 3
        public byte u30 { get; set; } //max 0x0F 15
        public byte u31 { get; set; } //max 0x0F 15
        public byte u40 { get; set; } //max 0xFF 255
        public byte u50 { get; set; } //max 0xFF 255

        public List<RdfVoiceClip> VoiceClips = new List<RdfVoiceClip>();
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            LabelName = new FoxHash();
            LabelName.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"Label name: {LabelName.HashValue}");

            byte[] flags = reader.ReadBytes(6);
            Console.WriteLine($"    flags: {flags[0]} {flags[1]} {flags[2]} {flags[3]} {flags[4]} {flags[5]}");

            u00 = (byte)(flags[0] & 0x0F ); //max 0x0F 15
            NoiseType = (byte)(flags[0] >> 4); //todo bits max 0x0F 15
                                               // if 0, short real time noise. 
                                               // if 2, haves espionage radio noise. 0b0010
                                               // if 4, optional radio noise, 0b0100
                                               // if 8, map radio 0b1000
                                               // if 10, mission image radio 0b1010

            u10 = (byte)(flags[1] & 0x0F); //max 0x0F 15
            u11 = (byte)(flags[1] >> 4); //max 0x0F 15

            var voiceClipCount = flags[2] & 0x3F; //max 0x3F 63
            u21 = (byte)(flags[2] >> 6); //max 0x03 3

            u30 = (byte)(flags[3] & 0x0F); //max 0x0F 15
            u31 = (byte)(flags[3] >> 4); //max 0x0F 15
            Console.WriteLine($"     u00: {u00} noiseType: {NoiseType}");
            Console.WriteLine($"     u10: {u10} u11: {u11}");
            Console.WriteLine($"     voiceClipCount: {voiceClipCount} u21: {u21}");
            Console.WriteLine($"     u30: {u30} u31: {u31}");
            Console.WriteLine($"     u40: {u40}");
            Console.WriteLine($"     u50: {u50}");

            u40 = flags[4]; //max 0xFF 255

            u50 = flags[5]; //max 0xFF 255

            for (int i = 0; i < voiceClipCount; i++)
            {
                RdfVoiceClip voiceClip = new RdfVoiceClip();
                voiceClip.Read(reader, hashManager, hashIdentifiedCallback);
                VoiceClips.Add(voiceClip);
            }
        }
        public void Write(BinaryWriter writer)
        {
            LabelName.Write(writer);
            byte[] flagbytes = new byte[6];
            Console.WriteLine($"     u00: {u00} noiseType: {NoiseType}");
            Console.WriteLine($"     u10: {u10} u11: {u11}");
            Console.WriteLine($"     voiceClipCount: {(byte)VoiceClips.Count} u21: {u21}");
            Console.WriteLine($"     u30: {u30} u31: {u31}");
            Console.WriteLine($"     u40: {u40}");
            Console.WriteLine($"     u50: {u50}");
            flagbytes[0] = (byte)((byte)(u00 & 0x0F) | (byte)((NoiseType & 0x0F) << 4));
            flagbytes[1] = (byte)((byte)(u10 & 0x0F) | (byte)((u11 & 0x0F) << 4));
            flagbytes[2] = (byte)((byte)(VoiceClips.Count & 0x3F) | (byte)((u21 | 0x04) << 6));
            flagbytes[3] = (byte)((byte)(u30 & 0x0F) | (byte)((u31 & 0x0F) << 4));
            flagbytes[4] = u40;
            flagbytes[5] = u50;
            Console.WriteLine($"     flagbytes {flagbytes[0]} {flagbytes[1]} {flagbytes[2]} {flagbytes[3]} {flagbytes[4]} {flagbytes[5]}");
            writer.Write(flagbytes);

            foreach (RdfVoiceClip voiceClip in VoiceClips)
            {
                voiceClip.Write(writer);
            }
        }
        public void ReadXml(XmlReader reader, List<FnvHash> dialogueEvents, List<FnvHash> voiceTypes)
        {
            LabelName = new FoxHash();
            LabelName.ReadXml(reader, "labelName");
            u00 = byte.Parse(reader["u00"]);
            NoiseType = byte.Parse(reader["noiseType"]);
            u10 = byte.Parse(reader["u10"]);
            u11 = byte.Parse(reader["u11"]);
            u21 = byte.Parse(reader["u21"]);
            u30 = byte.Parse(reader["u30"]);
            u31 = byte.Parse(reader["u31"]);
            u40 = byte.Parse(reader["u40"]);
            u50 = byte.Parse(reader["u50"]);

            Console.WriteLine($"Label name: {LabelName.HashValue}");
            Console.WriteLine($"         u00: {u00} noiseType: {NoiseType}");
            Console.WriteLine($"         u10: {u10} u11: {u11}");
            Console.WriteLine($"         u21: {u21} u30: {u30}");
            Console.WriteLine($"         u31: {u31}");
            Console.WriteLine($"         u40: {u40}");
            Console.WriteLine($"         u50: {u50}");
            bool doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("label");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfVoiceClip voiceClip = new RdfVoiceClip();
                        voiceClip.ReadXml(reader, dialogueEvents, voiceTypes);
                        VoiceClips.Add(voiceClip);
                        Console.WriteLine($"    {voiceClip.VoiceId.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        return;
                }
        }
        public void WriteXml(XmlWriter writer, List<FnvHash> dialogueEvents, List<FnvHash> voiceTypes)
        {
            writer.WriteStartElement("label");
            LabelName.WriteXml(writer, "labelName");
            writer.WriteAttributeString("u00", u00.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("noiseType", NoiseType.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u10", u10.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u11", u11.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u21", u21.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u30", u30.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u31", u31.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u40", u40.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u50", u50.ToString(CultureInfo.InvariantCulture));

            Console.WriteLine($"Label name: {LabelName.StringLiteral}");
            foreach (RdfVoiceClip voiceClip in VoiceClips)
            {
                voiceClip.WriteXml(writer, dialogueEvents, voiceTypes);
            }
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema() { return null; }
    }
}
