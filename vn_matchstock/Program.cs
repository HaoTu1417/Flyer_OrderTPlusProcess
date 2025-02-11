// Decompiled with JetBrains decompiler
// Type: vn_matchstock.Program
// Assembly: vn_matchstock, Version=2023.10.31.237, Culture=neutral, PublicKeyToken=null
// MVID: F04EE2D5-AF78-4C81-9699-CA665C6E41C5
// Assembly location: /Users/tunghaotu/www/service/vn_matchstock/vn_matchstock.dll

using Common.Utility;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace vn_matchstock
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Log.Info("Start");
                if (args.Length == 0)
                {
                    Log.Info("Please specify market name");
                }
                else
                {
                    string market_name = args[0];
                    Market market = (new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).
                        AddJsonFile("appsettings.json").
                        Build().
                        GetSection("Markets")
                        .Get<List<Market>>() ?? new List<Market>()).First<Market>((Func<Market, bool>) (m => m.name == market_name));
                    if (args.Length > 1 && args[1] == "test")
                    {
                        new Worker(market).Test();
                    }
                    else
                    {
                        new Worker(market).Run();
                        Log.Info("End");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}