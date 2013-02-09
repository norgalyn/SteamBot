using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot
{
    public struct Request
    {
        public SteamID User;
        public int Priority, ID;
        public Bot.TradeTypes TradeType;
        public string[] Data;
        public Request(int id, SteamID user, int priority, int tradetype, string[] data)
        {
            ID = id;
            User = user;
            Priority = priority;
            TradeType = (Bot.TradeTypes)tradetype;
            Data = data;
        }
    }
}
