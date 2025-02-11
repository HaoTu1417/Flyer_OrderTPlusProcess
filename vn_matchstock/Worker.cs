// Decompiled with JetBrains decompiler
// Type: vn_matchstock.Worker
// Assembly: vn_matchstock, Version=2023.10.31.237, Culture=neutral, PublicKeyToken=null
// MVID: F04EE2D5-AF78-4C81-9699-CA665C6E41C5
// Assembly location: /Users/tunghaotu/www/service/vn_matchstock/vn_matchstock.dll

using Common.Helper;
using Common.Models;
using Common.Models.Dto;
using Common.Services;
using Common.Utility;
using Common.Utility.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;

#nullable enable
namespace vn_matchstock
{
  public class Worker
  {
    private readonly Market _market;

    public Worker(Market market) => this._market = market;

    public void Run()
    {
      TradeTime tradeTime = new TradeTime(this._market.name);
      while (true)
      {
        DateTime time = Tool.GetTime(this._market.name);
        if (!(time.TimeOfDay > this._market.end_time))
        {
          foreach (Exchange exchange in this._market.exchanges)
          {
            try
            {
              DateTime time_limit;
              if (tradeTime.IsMatchTime(time, exchange, out time_limit))
              {
                this.Match(exchange, time_limit);
                this.ProcessDelivery();
              }
            }
            catch (Exception ex)
            {
              Log.Error(ex.Message, ex);
            }
          }
          Thread.Sleep(this._market.interval);
        }
        else
          break;
      }
    }

    public void Test()
    {
      while (true)
      {
        foreach (Exchange exchange in this._market.exchanges)
        {
          try
          {
            this.Match(exchange, DateTime.UtcNow.AddHours(1.0));
            this.ProcessDelivery();
          }
          catch (Exception ex)
          {
            Log.Error(ex.Message, ex);
          }
        }
        Thread.Sleep(this._market.interval);
      }
    }

    public void Match(Exchange exchange, DateTime time_limit)
    {
      List<TradeOrderDto> openOrders = this.GetOpenOrders(exchange.name);
      DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(16, 2);
      interpolatedStringHandler1.AppendLiteral("交易所: ");
      interpolatedStringHandler1.AppendFormatted(exchange.name);
      interpolatedStringHandler1.AppendLiteral(" 開始撮合(數量: ");
      interpolatedStringHandler1.AppendFormatted<int>(openOrders.Count);
      interpolatedStringHandler1.AppendLiteral(")");
      Log.Info(interpolatedStringHandler1.ToStringAndClear());
      foreach (TradeOrderDto order in openOrders)
      {
        try
        {
          if (order.order_time >= time_limit)
          {
            DefaultInterpolatedStringHandler interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(16, 3);
            interpolatedStringHandler2.AppendLiteral("單號: ");
            interpolatedStringHandler2.AppendFormatted(order.sn);
            interpolatedStringHandler2.AppendLiteral(" 的 ");
            interpolatedStringHandler2.AppendFormatted<DateTime>(order.order_time, "yyyy-MM-dd HH:mm:ss");
            interpolatedStringHandler2.AppendLiteral(" > ");
            interpolatedStringHandler2.AppendFormatted<DateTime>(time_limit, "yyyy-MM-dd HH:mm:ss");
            interpolatedStringHandler2.AppendLiteral(", 不予撮合");
            Log.Debug(interpolatedStringHandler2.ToStringAndClear());
          }
          else
            Worker.ProcessOrder(order);
        }
        catch (Exception ex)
        {
          Log.Error(ex.Message, ex);
        }
      }
      Log.Info("交易所: " + exchange.name + " 撮合完成");
    }

    public void ProcessDelivery()
    {
      List<TradeOrderDto> undeliveredOrders = this.GetUndeliveredOrders();
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
      interpolatedStringHandler.AppendLiteral("開始執行交割程序(數量: ");
      interpolatedStringHandler.AppendFormatted<int>(undeliveredOrders.Count);
      interpolatedStringHandler.AppendLiteral(")");
      Log.Info(interpolatedStringHandler.ToStringAndClear());
      foreach (TradeOrderDto order in undeliveredOrders)
      {
        Log.Debug("單號: " + order.sn + " 開始交割");
        string subAccount = order.sub_account;
        try
        {
          if (order.succeed_volume > 0)
          {
            TradeAccountDto account = TradeAccountService.Find(subAccount);
            using (TransactionScope transactionScope = new TransactionScope())
            {
              DeliveryHelper.Delivery(order, account, this._market.is_allowed_daytrading);
              transactionScope.Complete();
              Log.Debug("交割成功");
            }
          }
        }
        catch (Exception ex)
        {
          Log.Debug("交割失敗, 原因: " + ex.Message);
          if (!TradeAccountService.IsExists(subAccount))
          {
            Log.Debug("交易帳戶" + subAccount + "不存在");
            TradeOrderService.Remove(order.pk);
          }
        }
      }
      Log.Info("結束交割程序");
    }

    private static void ProcessOrder(TradeOrderDto order)
    {
      Log.Debug("單號: " + order.sn + " 開始撮合");
      if (!StockQuoteService.IsExists(order.market, order.stock_code))
      {
        Log.Debug("股票代碼: " + order.stock_code + " 不存在");
      }
      else
      {
        StockQuote stockQuote = StockQuoteService.GetStockQuote(order.market, order.stock_code);
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 4);
        interpolatedStringHandler.AppendLiteral("股票代碼: ");
        interpolatedStringHandler.AppendFormatted(stockQuote.stock_code);
        interpolatedStringHandler.AppendLiteral(" 市價: ");
        interpolatedStringHandler.AppendFormatted<Decimal>(stockQuote.price);
        interpolatedStringHandler.AppendLiteral(" 委買:");
        interpolatedStringHandler.AppendFormatted(stockQuote.bids.Serialize());
        interpolatedStringHandler.AppendLiteral(" 委賣:");
        interpolatedStringHandler.AppendFormatted(stockQuote.asks.Serialize());
        Log.Debug(interpolatedStringHandler.ToStringAndClear());
        if (order.order_time >= stockQuote.update_time)
        {
          interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 3);
          interpolatedStringHandler.AppendLiteral("單號: ");
          interpolatedStringHandler.AppendFormatted(order.sn);
          interpolatedStringHandler.AppendLiteral(" 的 ");
          interpolatedStringHandler.AppendFormatted<DateTime>(order.order_time, "yyyy-MM-dd HH:mm:ss");
          interpolatedStringHandler.AppendLiteral(" > ");
          interpolatedStringHandler.AppendFormatted<DateTime>(stockQuote.update_time, "yyyy-MM-dd HH:mm:ss");
          interpolatedStringHandler.AppendLiteral(", 不予撮合");
          Log.Debug(interpolatedStringHandler.ToStringAndClear());
        }
        else if (TradeAccountService.Find(order.sub_account) == null)
        {
          TradeOrderService.Remove(order.pk);
        }
        else
        {
          Worker.LogDebugMsg(order);
          Decimal final_price = 0M;
          int final_volume = 0;
          switch (order.dir)
          {
            case 1:
              if (stockQuote.asks == null || stockQuote.asks.Length == 0)
              {
                Log.Debug("沒有賣單, 無法成交");
                break;
              }
              switch (order.price_type)
              {
                case 1:
                  if (order.price >= stockQuote.price || order.price >= stockQuote.asks[0])
                  {
                    final_price = Math.Min(stockQuote.price, stockQuote.asks[0]);
                    final_volume = order.free_volume;
                    break;
                  }
                  break;
                case 2:
                  final_price = stockQuote.price;
                  final_volume = order.free_volume;
                  break;
                case 3:
                  if (order.price <= stockQuote.price)
                  {
                    final_price = order.price;
                    final_volume = order.free_volume;
                    break;
                  }
                  break;
                default:
                  TradeOrderService.Remove(order.pk);
                  interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                  interpolatedStringHandler.AppendLiteral("不支援的價格類型: ");
                  interpolatedStringHandler.AppendFormatted<int>(order.price_type);
                  throw new Exception(interpolatedStringHandler.ToStringAndClear());
              }
              break;
            case 2:
              if (stockQuote.bids == null || stockQuote.bids.Length == 0)
              {
                Log.Debug("沒有買單, 無法成交");
                break;
              }
              switch (order.price_type)
              {
                case 1:
                  if (order.price <= stockQuote.price || order.price <= stockQuote.bids[0])
                  {
                    final_price = Math.Max(stockQuote.price, stockQuote.bids[0]);
                    final_volume = order.free_volume;
                    break;
                  }
                  break;
                case 2:
                  final_price = stockQuote.price;
                  final_volume = order.free_volume;
                  break;
                case 3:
                  if (order.price >= stockQuote.price)
                  {
                    final_price = order.price;
                    final_volume = order.free_volume;
                    break;
                  }
                  break;
                default:
                  TradeOrderService.Remove(order.pk);
                  interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                  interpolatedStringHandler.AppendLiteral("不支援的價格類型: ");
                  interpolatedStringHandler.AppendFormatted<int>(order.price_type);
                  throw new Exception(interpolatedStringHandler.ToStringAndClear());
              }
              break;
          }
          interpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
          interpolatedStringHandler.AppendLiteral("[撮合結果]: 成交價: ");
          interpolatedStringHandler.AppendFormatted<Decimal>(final_price);
          interpolatedStringHandler.AppendLiteral(" 成交量: ");
          interpolatedStringHandler.AppendFormatted<int>(final_volume);
          Log.Debug(interpolatedStringHandler.ToStringAndClear());
          if (final_volume > 0)
          {
            Log.Debug("成交");
            Worker.AfterOrderFilled(order, final_price, final_volume);
          }
          else
          {
            Log.Debug("未成交");
            if (order.price_type == 2)
              OrderHelper.CancelOrder(order.pk, 2, "matchstock");
          }
          Log.Debug("單號: " + order.sn + " 結束撮合");
        }
      }
    }

    private static void AfterOrderFilled(
      TradeOrderDto order,
      Decimal final_price,
      int final_volume)
    {
      Decimal num1 = final_price * (Decimal) final_volume;
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 3);
      interpolatedStringHandler.AppendLiteral("單號: ");
      interpolatedStringHandler.AppendFormatted(order.sn);
      interpolatedStringHandler.AppendLiteral(" 成交, 成交價: ");
      interpolatedStringHandler.AppendFormatted<Decimal>(final_price);
      interpolatedStringHandler.AppendLiteral(" 成交量: ");
      interpolatedStringHandler.AppendFormatted<int>(final_volume);
      Log.Debug(interpolatedStringHandler.ToStringAndClear());
      Decimal num2 = order.avg_price * (Decimal) order.succeed_volume;
      order.avg_price = (num2 + num1) / (Decimal) (order.succeed_volume + final_volume);
      order.free_volume -= final_volume;
      order.succeed_volume += final_volume;
      order.last_price = final_price;
      OrderHelper.EvaluateOrderStatus(order);
      TradeOrderService.Update(order);
    }

    private static void LogDebugMsg(TradeOrderDto order)
    {
      if (!Log.IsDebugEnabled)
        return;
      int dir = order.dir;
      if (true)
        ;
      string str1;
      switch (dir)
      {
        case 1:
          str1 = "買進";
          break;
        case 2:
          str1 = "賣出";
          break;
        default:
          str1 = "未知";
          break;
      }
      if (true)
        ;
      string str2 = str1;
      int priceType = order.price_type;
      if (true)
        ;
      string str3;
      switch (priceType)
      {
        case 1:
          str3 = "限價";
          break;
        case 2:
          str3 = "市價";
          break;
        case 3:
          str3 = "停損";
          break;
        default:
          str3 = "未知";
          break;
      }
      if (true)
        ;
      string str4 = str3;
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 5);
      interpolatedStringHandler.AppendFormatted(str4);
      interpolatedStringHandler.AppendFormatted(str2);
      interpolatedStringHandler.AppendLiteral(" ");
      interpolatedStringHandler.AppendFormatted(order.stock_code);
      interpolatedStringHandler.AppendLiteral(" 價: ");
      interpolatedStringHandler.AppendFormatted<Decimal>(order.price);
      interpolatedStringHandler.AppendLiteral(" 量: ");
      interpolatedStringHandler.AppendFormatted<int>(order.free_volume);
      Log.Debug(interpolatedStringHandler.ToStringAndClear());
    }

    private List<TradeOrderDto> GetOpenOrders(string exchange_name)
    {
      List<TradeOrderDto> openOrders = TradeOrderService.GetOpenOrders(this._market.name);
      HashSet<string> stock_codes_by_exchange = StockService.GetStockCodesByExchange(this._market.name, exchange_name).ToHashSet<string>();
      return openOrders.Where<TradeOrderDto>((Func<TradeOrderDto, bool>) (x => stock_codes_by_exchange.Contains(x.stock_code))).ToList<TradeOrderDto>();
    }

    private List<TradeOrderDto> GetUndeliveredOrders()
    {
      return TradeOrderService.GetFilledOrders(this._market.name).Where<TradeOrderDto>((Func<TradeOrderDto, bool>) (x => !TradeDealService.IsExists(x.sn))).ToList<TradeOrderDto>();
    }
  }
}
