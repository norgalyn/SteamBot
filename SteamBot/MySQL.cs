using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MySql.Data;
using MySql.Data.MySqlClient;
using SteamTrade;

namespace SteamBot
{
    static public class MySQL
{
        public enum RequestStatus
        {
            Waiting,
            Proccesing,
            Success,
            Timedout,
            Canceled,
            AdminAbort
        }

        static Log log = new Log("Mysql.log", "MySQL", Log.LogLevel.Debug);
        //online access http://82.117.152.138/adminer/
        //webpage http://82.117.152.138/hats.tf
        static MySqlConnection conn = new MySqlConnection("server=82.117.152.138;user=brseker_norgy;database=brseker_hats_tf;port=3306;password=URFBu5vawYKmGLZG;");
        static MySqlConnection QueueConnection = new MySqlConnection("server=82.117.152.138;user=brseker_norgy;database=brseker_hats_tf;port=3306;password=URFBu5vawYKmGLZG;");

        static public void start()
        {
            conn.Open();
            QueueConnection.Open();
        }

        static public Request getItem()
        {
        Request r = new Request();
            try
            {
                MySqlDataReader rdr = new MySqlCommand("SELECT `id`,`steam_id`,`type`,`priority` FROM `queue` WHERE `id` IN (SELECT `queue_id` FROM `queue_status` WHERE `active`=1 AND `status`=" + (int)RequestStatus.Waiting + ") ORDER BY `priority` DESC, `id` ASC LIMIT 1", QueueConnection).ExecuteReader();
                while (rdr.Read())
                {
                    r.ID = rdr.GetInt32(0);
                    r.User = rdr.GetUInt64(1);
                    r.TradeType = (Bot.TradeTypes)rdr.GetInt32(2);
                    r.Priority = rdr.GetInt32(3);
                }
                rdr.Close();
                r.Data = new string[Int32.Parse((new MySqlCommand("SELECT COUNT(`data`) FROM `queue_items` WHERE `queue_id`=" + r.ID, conn)).ExecuteScalar().ToString())];
                rdr = (new MySqlCommand("SELECT `data` FROM `queue_items` WHERE `queue_id`=" + r.ID, conn)).ExecuteReader();
                int i = 0;
                while (rdr.Read())
                    r.Data[i++] = rdr.GetString(0);
                rdr.Close();
            }
            catch (Exception e)
            {
                log.Error("Error getting new request: " + e.Message);
            }

            return r;
        }

        static public void assignRequest(Bot bot)
        {
            new Thread(() =>
                {
                    while (true)
                    {
                        if (getItem().User!=null)
                        {
                            bot.DoRequest(getItem());
                            break;
                        }
                        Thread.Sleep(5000);
                    }
                }).Start();

        }

         static public void wipeBotInventory(SteamKit2.SteamID botID)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM bot_inventory WHERE bot_id="+botID.ConvertToUInt64(), conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error("Error wiping cache inventory: " + e.Message);
            }
        }
        

        static public void setBotInventory(SteamKit2.SteamID botID, int defindex, int count)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(String.Format("INSERT INTO bot_inventory (bot_id,defindex,count) VALUES ({0},{1},{2}) ON DUPLICATE KEY UPDATE count={2};", botID.ConvertToUInt64(), defindex, count), conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error("Error caching inventory: " + e.Message);
            }
        }
        
        static public void setRequestStatus(int id, RequestStatus status)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(String.Format("CALL update_status({0},{1})",id, (int)status), conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error("Error updating request status: " + e.Message);
            }
        }

        static public int getUserRank(SteamKit2.SteamID id)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand("SELECT `rank_id` FROM users WHERE steam_id=" + id.ConvertToUInt64(), conn);
                return (int)cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                log.Error("Error updating user rank: " + e.Message);
            }
            return -1;
        }

        static public void addRequestData(int id, string data)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand("INSERT INTO queue_items(queue_id,data) VALUES (" + id + "," + data + ")", conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error("Error adding request data: " + e.Message);
            }
        }

        static public string getData(string name)
        {
            try
            {
                return (string)(new MySqlCommand("SELECT `value` FROM data WHERE name='" + name+"'", conn).ExecuteScalar());       
            }
            catch (Exception e)
            {
                log.Error("Error getting data: " + e.Message);
                return null;
            }
        }

        static public void updateSchema()
        {
            foreach(Schema.Item item in Trade.CurrentSchema.Items)
            {
                try
                {
                    new MySqlCommand(String.Format("INSERT INTO items (defindex, name, image, craft_class) VALUES ({0},'{1}','{2}','{3}') ON DUPLICATE KEY UPDATE image='{2}',craft_class='{3}';", item.Defindex,MySqlHelper.EscapeString(item.ItemName), MySqlHelper.EscapeString(item.ImageUrl),item.CraftMaterialType), conn).ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error("Error updating schema: " + e.Message);
                }
            }
            setData("schema_version", Trade.CurrentSchema.GetHashCode().ToString());
        }

        static public void setData(string name, string data)
        {
            try
            {
                new MySqlCommand(String.Format("UPDATE data SET value='{1}' WHERE name='{0}'", name, data), conn).ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error("Error setting data: " + e.Message);
            }
        }
    }
}
