using UnityEngine;

/// <summary>
/// 统一管理游戏世界渲染顺序。Ships 层内从低到高：背景 → 飞船 → 船上乘客 → 站点 → 站台乘客 → 过载条。
/// 站台乘客高于站点，避免被站点形状遮挡；船上乘客与站台乘客分层，停靠时减少重叠。
/// </summary>
public static class SortingOrderConstants
{
    public const int Background = -100;
    public const int ShipCarriageIndicator = 4;
    public const int Ship = 5;
    public const int Passenger = 8;       // 船上乘客
    public const int Station = 10;
    public const int StationPassenger = 11; // 站台乘客，高于站点避免被遮挡
    public const int OverloadBarBg = 17;
    public const int OverloadBarFill = 18;

    private static int _shipsLayerId = int.MinValue;
    public static int ShipsLayerId
    {
        get
        {
            if (_shipsLayerId == int.MinValue)
            {
                _shipsLayerId = SortingLayer.NameToID("Ships");
                if (_shipsLayerId < 0) _shipsLayerId = 0;
            }
            return _shipsLayerId;
        }
    }
}
