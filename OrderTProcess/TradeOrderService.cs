using System.Data;
using Common.Utility;
using Dapper;

namespace OrderTProcess;

public class TradeOrderService: Common.Services.TradeOrderService
{
    /// <summary>
    /// Get the order that haven't completed the T+ condition.
    /// </summary>
    /// <param name="market"></param>
    /// <param name="subaccount"></param>
    /// <param name="stockCode"></param>
    /// <returns></returns>
    public static List<TradeOrderDto> GetPendingTPlusOrders(string market, string subaccount,string stockCode)
    {
        string sql = "SELECT * FROM trade_order WHERE market = @market AND sub_account = @subaccount AND stock_code = @stockCode And TPlus<3 AND dir=1 AND status =1 or status =4 ORDER BY order_time, pk";
        var data = new{ market = market, subaccount = subaccount, stockCode = stockCode };
        using (IDbConnection readConnection = DapperMysql.GetReadConnection())
            return readConnection.Query<TradeOrderDto>(sql, (object) data).ToList<TradeOrderDto>();
    }
    
    public static void Update2(TradeOrderDto order)
    {
        string sql = "UPDATE `trade_order` SET `succeed_volume` = @succeed_volume, `free_volume` = @free_volume, `cancel_volume` = @cancel_volume,`last_price` = @last_price, `avg_price` = @avg_price, `status` = @status,`TPlus` =@TPlus WHERE `pk` = @pk";
        using (IDbConnection writeConnection = DapperMysql.GetWriteConnection())
            writeConnection.Execute(sql, (object) order);
    }
    
    
}