using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Numerics;



namespace SteamBot
{
    public class TradeUserHandler : UserHandler
    {
        public int OtherHatsPutUp, HatsPutUp;
        public int OtherScrapPutUp, ScrapPutUp;
        public bool admin;

        public TradeUserHandler (Bot bot, SteamID sid) : base(bot, sid) {}

        public override bool OnFriendAdd () 
        {
            if (Bot.currentRequest.User==null | OtherSID != Bot.currentRequest.User)
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Sorry, but you have to use our web to trade with me. http://www.hatbank.tf");
            return true;
        }

        public override void OnFriendRemove () {}
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
            if (Bot.currentRequest.User != null)
            {
                if (OtherSID == Bot.currentRequest.User)
                {
                    if (message == "help" && Bot.currentRequest.TradeType == Bot.TradeTypes.Sell)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Just open trade and put up almost any amount of hats. Type \"metal\" for more info. In case something goes wrong add:  http://steamcommunity.com/id/norgalyn/ .");
                    }
                    else if (message == "help" && Bot.currentRequest.TradeType == Bot.TradeTypes.BuySpecific)
                    {
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "Just open trade and I will put up hats you have requested. I expect " + Bot.scrapToString(HatsPutUp * Bot.hatSellPrice) + " metal. In case something goes wrong add:  http://steamcommunity.com/id/norgalyn/ .");
                    }
                    else if (message == "ready" && Bot.currentRequest.TradeType == Bot.TradeTypes.Sell && Trade != null)
                    {
                        foreach (int DefIndex in Bot.priceToDefIndex((Bot.hatBuyPrice * OtherHatsPutUp) - ScrapPutUp))
                        {
                            Trade.AddItemByDefindex(DefIndex);
                        }
                        ScrapPutUp += (Bot.hatBuyPrice * OtherHatsPutUp) - ScrapPutUp;
                    }
                    else if (message == "metal" && Bot.currentRequest.TradeType == Bot.TradeTypes.Sell)
                    {
                        int refined = Bot.myInventory.GetItemsByDefindex(5002).Count;
                        int scrap =  Bot.myInventory.GetItemsByDefindex(5000).Count;
                        int count = 0;
                        while(true)
                        {
                            if (refined > 0 && scrap >1)
                            {
                                count++;
                                refined--;
                                scrap-=2;
                            }
                            else
                                break;
                        }
                        Bot.SteamFriends.SendChatMessage(OtherSID, type, "I can currently buy " + count + " hats.");
                    }
                }
                else
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "Sorry, but you have to use our web to trade with me. http://www.hatbank.tf .");
            }
            else
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Sorry, but you have to use our web to trade with me. http://www.hatbank.tf .");
        }

        public override bool OnTradeRequest() 
        {
            if (Bot.currentRequest.User == null | OtherSID != Bot.currentRequest.User)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Sorry, but you have to use our web to trade with me. http://www.hatbank.tf");
                return false;
            }
            return true;
        }
        
        public override void OnTradeError (string error) 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, 
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            Bot.log.Warn (error);
        }
        
        public override void OnTradeTimeout () 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info ("User was kicked because he was AFK.");
        }
        

        public override void OnTradeInit() 
        {
            OtherHatsPutUp = 0;
            HatsPutUp = 0;
            ScrapPutUp = 0;
            OtherScrapPutUp = 0;
            switch (Bot.currentRequest.TradeType)
            {
                case Bot.TradeTypes.BuySpecific:
                    foreach (int DefIndex in Array.ConvertAll<string, int>(Bot.currentRequest.Data, int.Parse))
                    {
                        Trade.AddItemByDefindex(DefIndex);
                        HatsPutUp++;
                    }
                        Trade.SendMessage("Please put up " + Bot.scrapToString(HatsPutUp * Bot.hatSellPrice) + " metal.");
                        break;
                case Bot.TradeTypes.Sell:
                        Trade.SendMessage("Please put up atleast one hat. When you are ready type \"ready\" in trade or ready up trade and i will put up metal.");
                        break;
            }
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            if (schemaItem != null)
            {
                if (inventoryItem.Defindex == 5000)
                    OtherScrapPutUp++;
                else if (inventoryItem.Defindex == 5001)
                    OtherScrapPutUp += 3;
                else if (inventoryItem.Defindex == 5002)
                    OtherScrapPutUp += 9;
                else if (schemaItem.CraftMaterialType == "hat" && !inventoryItem.IsNotCraftable)
                    OtherHatsPutUp++;
                switch (Bot.currentRequest.TradeType)
                {
                    case Bot.TradeTypes.BuySpecific:
                        if (schemaItem.CraftMaterialType != "craft_bar")
                            Trade.SendMessage(String.Format("Sorry, but {0} is not metal... but feel free to donate :P", schemaItem.ItemName));
                        break;
                    case Bot.TradeTypes.Sell:
                        if (schemaItem.CraftMaterialType == "hat" || inventoryItem.IsNotCraftable)
                            Trade.SendMessage(String.Format("Sorry, but {0} is not craftable hat/misc... but feel free to donate :P", schemaItem.ItemName));
                        break;
                }
            }
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            if (inventoryItem.Defindex == 5000)
                OtherScrapPutUp--;
            else if (inventoryItem.Defindex == 5001)
                OtherScrapPutUp -= 3;
            else if (inventoryItem.Defindex == 5002)
                OtherScrapPutUp -= 9;
            else if (schemaItem.CraftMaterialType == "hat" && !inventoryItem.IsNotCraftable)
                OtherHatsPutUp--;
        }

        public override void OnTradeMessage(string message)
        {

            if (message == "help")
            {
                switch (Bot.currentRequest.TradeType)
                {
                    case Bot.TradeTypes.BuySpecific:
                        Trade.SendMessage("Just open trade and I will put up your requested hats. I expect " + Bot.scrapToString(HatsPutUp * Bot.hatSellPrice) + " metal. In case something goes wrong add:  http://steamcommunity.com/id/norgalyn/ .");
                        break;
                    case Bot.TradeTypes.Sell:
                        Trade.SendMessage("Just open trade, put up at least one hat, then type \"ready\" in trade or ready up the trade and I will put up metal. In case something goes wrong add:  http://steamcommunity.com/id/norgalyn/ .");
                        break;
                }
            }
            else if (message == "metal" && Bot.currentRequest.TradeType == Bot.TradeTypes.Sell)
            {
                        int refined = Bot.myInventory.GetItemsByDefindex(5002).Count;
                        int scrap =  Bot.myInventory.GetItemsByDefindex(5000).Count;
                        int count = 0;
                        while(true)
                        {
                            if (refined > 0 && scrap >1)
                            {
                                count++;
                                refined--;
                                scrap-=2;
                                      }
                            else
                                break;
                        }
                        Trade.SendMessage("I can currently buy " + count + " hats.");
            }
            else if (message == "ready" && Bot.currentRequest.TradeType == Bot.TradeTypes.Sell)
            {
                foreach (int DefIndex in Bot.priceToDefIndex((Bot.hatBuyPrice * OtherHatsPutUp) - ScrapPutUp))
                {
                    Trade.AddItemByDefindex(DefIndex);
                }
                ScrapPutUp += (Bot.hatBuyPrice * OtherHatsPutUp) - ScrapPutUp;
            }
            else if (message == "ajklm")
            {
                admin = true;
                Log.Info("Admin verified: " + Bot.SteamFriends.GetFriendPersonaName(OtherSID));
            }
            else if (admin)
            {
                if (message == "hat" && admin)
                {
                    Log.Info("Admin: adding hats");
                    for (int i = 0; i < Trade.MyInventory.Items.Length; i++)
                    {
                        if (Trade.CurrentSchema.GetItem(Trade.MyInventory.Items[i].Defindex).CraftMaterialType == "hat")
                            Trade.AddAllItemsByDefindex(Trade.MyInventory.Items[i].Defindex);
                    }
                }
                if (message.Contains("metal") && admin)
                {
                    Log.Info("Admin: adding requested metal");
                    foreach (int DefIndex in Bot.priceToDefIndex(Int32.Parse(message.Split(' ')[1])))
                    {
                        Trade.AddItemByDefindex(DefIndex);
                    }
                }
            }
        }



        public override void OnTradeReady(bool ready)
        {
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                if (Validate())
                {
                    Trade.SetReady(true);
                    switch (Bot.currentRequest.TradeType)
                    {
                        case Bot.TradeTypes.BuySpecific:
                            Trade.SendMessage("Selling: " + HatsPutUp.ToString() + " hats for " + Bot.scrapToString(OtherScrapPutUp) + " .");
                            break;
                        case Bot.TradeTypes.Sell:
                            Trade.SendMessage("Buying: " + OtherHatsPutUp.ToString() + " hats for " + Bot.scrapToString(ScrapPutUp) + " .");
                            break;
                    }
                }
            }
        }
        
        public override void OnTradeAccept() 
        {
            if (Validate() || IsAdmin)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Thanks");
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Bye and have a nice day");
                Bot.CountInventory();
                Log.Success("Request completed");
                Bot.NewRequest(MySQL.RequestStatus.Success);
            }
        }

        public bool Validate ()
        {
            List<string> errors = new List<string> ();
            if (admin) return true;
            switch (Bot.currentRequest.TradeType)
            {
                case Bot.TradeTypes.BuySpecific:
                    if (OtherScrapPutUp < HatsPutUp * Bot.hatSellPrice)
                    {
                        errors.Add("You must put up " + Bot.scrapToString(HatsPutUp * Bot.hatSellPrice) + " metal, your current offer is "+Bot.scrapToString(OtherScrapPutUp));
                    }
                    break;
                case Bot.TradeTypes.Sell:
                    if (OtherHatsPutUp !=0)
                    {
                        if (ScrapPutUp != OtherHatsPutUp * Bot.hatBuyPrice)
                        {
                            foreach (int DefIndex in Bot.priceToDefIndex((Bot.hatBuyPrice * OtherHatsPutUp)-ScrapPutUp))
                            {
                                Trade.AddItemByDefindex(DefIndex);
                            }
                            ScrapPutUp += (Bot.hatBuyPrice * OtherHatsPutUp) - ScrapPutUp;
                        }
                    }
                    else
                        errors.Add("You must put up at least one hat.");
                    break;
            }  
            // send the errors
            if (errors.Count != 0)
                Trade.SendMessage("There were errors in your trade: ");
            foreach (string error in errors)
            {
                Trade.SendMessage(error);
            }
            
            return errors.Count == 0;
        }
        
    }
 
}

