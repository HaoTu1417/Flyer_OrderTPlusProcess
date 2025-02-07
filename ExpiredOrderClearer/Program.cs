// Decompiled with JetBrains decompiler
// Type: ExpiredOrderClearer.Program
// Assembly: ExpiredOrderClearer, Version=2023.11.8.255, Culture=neutral, PublicKeyToken=null
// MVID: A45BB7A3-CB98-435B-9842-3DA14BB80156
// Assembly location: /Users/tunghaotu/www/service/orderClear/ExpiredOrderClearer.dll

using Common.Utility;
using Microsoft.Extensions.Configuration;
using NLog;
using System;

#nullable enable
namespace ExpiredOrderClearer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting expired order clearer...");
            Log.Info("hello");
            try
            {
                 Log.Info("Start");
                if (args.Length == 0)
                {
                    Log.Error("Please specify market name");
                }
                else
                {
                    Log.Error("Market name"+args[0]);
                     var a = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json").Build();
                    new Worker(args[0].ToUpper()).Run();
                     Log.Info("End");
                }
               
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: " + ex.Message); // Ensure exception message is printed
                 Log.Fatal(ex);
            }
            finally
            {
                LogManager.Shutdown();
            }
            
            Console.WriteLine("Ending expired order clearer...");
        }
    }
}