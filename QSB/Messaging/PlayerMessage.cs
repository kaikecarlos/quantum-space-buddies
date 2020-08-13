﻿using UnityEngine.Networking;

namespace QSB.Messaging
{
    public class PlayerMessage : MessageBase
    {
        public uint SenderId { get; set; }
        
        public override void Deserialize(NetworkReader reader)
        {
            SenderId = reader.ReadUInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(SenderId);
        }
    }
}