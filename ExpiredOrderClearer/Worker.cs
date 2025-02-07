// Decompiled with JetBrains decompiler
// Type: ExpiredOrderClearer.Worker
// Assembly: ExpiredOrderClearer, Version=2023.11.8.255, Culture=neutral, PublicKeyToken=null
// MVID: A45BB7A3-CB98-435B-9842-3DA14BB80156
// Assembly location: /Users/tunghaotu/www/service/orderClear/ExpiredOrderClearer.dll

using Common.Helper;
using Common.Models.Dto;
using Common.Services;
using Common.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;

#nullable enable
namespace ExpiredOrderClearer
{
  public class Worker
  {
    private readonly string _market;

    public Worker(string market) => this._market = market;

    public void Run()
    {
      //TODO: add an if to check to do the job below if only the the T+ is valid
      
      // nếu để ở đây có nghĩa là nó đang mở, hết T+, và phải đến trưa hoặc chiều (hết giờ giao dịch mới chạy)
      // không đúng
      // Nếu đủ t+ thì delivery, còn nếu hết giờ giao dịch thì mới cancelorder
      List<TradeOrderDto> openOrdersByMarket = TradeOrderService.FindOpenOrdersByMarket(this._market);
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
      interpolatedStringHandler.AppendLiteral("Found ");
      interpolatedStringHandler.AppendFormatted<int>(openOrdersByMarket.Count);
      interpolatedStringHandler.AppendLiteral(" open orders");
      Log.Info(interpolatedStringHandler.ToStringAndClear());  
      
      
      
      List<TradeOrderDto> orderMatchTPlus = openOrdersByMarket.Where(order => order.order_time <= DateTime.Now.AddDays(-2.5))
        .ToList();
      
      DefaultInterpolatedStringHandler interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(18, 1);
      interpolatedStringHandler2.AppendLiteral("Found ");
      interpolatedStringHandler2.AppendFormatted<int>(openOrdersByMarket.Count);
      interpolatedStringHandler2.AppendLiteral(" open orders match Tplus condition");
      Log.Info(interpolatedStringHandler2.ToStringAndClear());  
      
      foreach (TradeOrderDto order in openOrdersByMarket)
      {
        try
        {
          Log.Debug("Cancel order " + order.sn);
          OrderHelper.CancelOrder(order.pk, 2, "expired");
          if (order.succeed_volume > 0)
          {
            TradeAccountDto account = TradeAccountService.Find(order.sub_account);
            using (TransactionScope transactionScope = new TransactionScope())
            {
              DeliveryHelper.Delivery(order, account, false);
              transactionScope.Complete();
            }
          }
        }
        catch (Exception ex)
        {
          Log.Error(ex.Message, ex);
        }
      }
      
      Log.Info("解除交易號凍結資金");
      TradeAccountService.ReleaseAllFrozenMoney();
      TradeFrozenService.RemoveAll();
      Log.Info("解除所有凍結部位");
      TradePositionService.ResetAllFrozenPos();
    }
  }
}
