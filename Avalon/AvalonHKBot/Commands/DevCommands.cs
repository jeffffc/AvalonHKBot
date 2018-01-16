﻿using Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace AvalonHKBot
{
    public partial class Commands
    {
        [Attributes.Command(Trigger = "sql", DevOnly = true)]
        public static void Sql(Message msg, string[] args)
        {
            if (args.Length == 1)
            {
                msg.Reply("You must enter a sql query.");
                return;
            }
            using (var db = new AvalonDb())
            {
                var conn = db.Database.Connection;
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                string raw = "";

                var queries = args[1].Split(';');
                foreach (var sql in queries)
                {
                    using (var comm = conn.CreateCommand())
                    {
                        comm.CommandText = sql;
                        if (string.IsNullOrEmpty(sql)) continue;
                        try
                        {
                            var reader = comm.ExecuteReader();
                            var result = "";
                            if (reader.HasRows)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    raw += $"<code>{reader.GetName(i).FormatHTML()}</code>" + (i == reader.FieldCount - 1 ? "" : " - ");
                                result += raw + Environment.NewLine;
                                raw = "";
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                        raw += (reader.IsDBNull(i) ? "<i>NULL</i>" : $"<code>{reader[i].ToString().FormatHTML()}</code>") + (i == reader.FieldCount - 1 ? "" : " - ");
                                    result += raw + Environment.NewLine;
                                    raw = "";
                                }
                            }

                            result += reader.RecordsAffected == -1 ? "" : (reader.RecordsAffected + " records affected");
                            result = !String.IsNullOrEmpty(result) ? result : (sql.ToLower().StartsWith("select") ? "Nothing found" : "Done.");
                            msg.Reply(result);
                        }
                        catch (Exception e)
                        {
                            msg.Reply(e.Message);
                        }
                    }
                }
            }
        }
    }
}
