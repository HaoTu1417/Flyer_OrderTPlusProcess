using System.Transactions;
using Common.Models.Dto;
using Common.Services;
using Common.Utility;
using NLog;

namespace OrderTProcess;

public class Worker
{
    // Danh sách các ngày nghỉ lễ (cần cập nhật hàng năm)
    private static List<DateTime> PublicHolidays;


    private readonly string _market;
    private readonly int _tplusDay;

    public Worker(string market, int tplusDay)
    {
        this._market = market;
        this._tplusDay = tplusDay;
    } 

    public void Run()
    {
        Log.Info("Starting Order TProcess");
        List<TradePositionDto> newTradePostions =
            TradePositionService.FindByMarket(this._market).Where(order => order.new_pos > 0).ToList();
        Log.Info("Found " + newTradePostions.Count + " unfinished orders");
        
       
        // if there is no newTradePostion no need to query the holidays
        // because the CalculateTPlusDays never been called.
        if (newTradePostions.Count > 0)
        {
            var holidays =  StockHolidayService.FindHolidays(_market);
            PublicHolidays = holidays.ToList();
        }
        foreach (var tradePosition in newTradePostions)
        {
            var tplusOrders =
                TradeOrderService.GetPendingTPlusOrders(_market, tradePosition.sub_account, tradePosition.stock_code);
            foreach (var tplusOrder in tplusOrders)
            {
                // Begin transaction using TransactionScope
                using (var scope = new TransactionScope(TransactionScopeOption.Required,
                           new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    try
                    {
                        // t+ time calculation
                        int tPlusDay = CalculateTPlusDays(tplusOrder.order_time);
                        Log.Info($"{tplusOrder.stock_code} sn {tplusOrder.sn} tPlusDay {tPlusDay}");

                        tplusOrder.TPlus = tPlusDay;

                        
                        // // Update the order
                        TradeOrderService.Update2(tplusOrder);
                        
                        // if the tplus is >= 3 proccess it.
                        if (tPlusDay >= _tplusDay)
                        {
                            tradePosition.new_pos -= tplusOrder.volume;
                        
                            // Update the trade position
                            TradePositionService.Update(tradePosition);
                        
                            Log.Info(
                                $"{tplusOrder.stock_code} of {tradePosition.sub_account} is finished, released volume {tplusOrder.volume}");
                        }
                        
                        // Commit the transaction
                        scope.Complete();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to process {tplusOrder.stock_code} for account {tradePosition.sub_account}. Rolling back.",ex);
                        // No need to explicitly roll back; TransactionScope will auto-rollback if Complete() isn't called
                    }
                }
            }
        }

        Log.Info("Ending Order TProcess");
    }

    public int CalculateTPlusDays(DateTime tradeDate)
    {
        int tPlusDays = 0;
        DateTime currentDate = tradeDate;
        DateTime refDay = DateTime.Now;

        while (currentDate < refDay.Date)
        {
            currentDate = currentDate.AddDays(1);

            // Kiểm tra nếu là thứ 7 hoặc CN hoặc ngày nghỉ lễ
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday &&
                !PublicHolidays.Contains(currentDate))
            {
                tPlusDays++;
            }
        }

        // Kiểm tra thời gian trong ngày hiện tại để tính 0.5 T+
        // if (refDay.TimeOfDay >= new TimeSpan(12, 0, 0)) // Sau 12h trưa
        // {
        //     return tPlusDays + 0.5;
        // }

        return tPlusDays;
    }
}