using System;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class SystemTimeExtension
{
    private const sbyte Hour = 12;

    public static DateTime ZoneManila(this DateTime dateTime)
    {
        return Convert.ToDateTime(dateTime).AddHours(Hour);
    }
}