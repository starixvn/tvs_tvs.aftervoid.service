using System;
using Fanex.Logging;
using StackExchange.Redis;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Utilities.CustomServices;

public static class LazyRedisMultiplexer
{
    private static ConnectionMultiplexer redis;
    private static readonly object multiplexerLock = new();
    private const int CONNECT_RETRY = 1000;
    private const int CONNECT_TIMEOUT = 5000;
    private const int SYNC_TIMEOUT = 30000;
    private const int ASYNC_TIMEOUT = 30000;

    private static void ConnectRedis()
    {
        try
        {
            var redisConfig = GetRedisConfig();
            var oldRedis = redis;

            redis = ConnectionMultiplexer.Connect(redisConfig);
            redis.ConnectionFailed += Redis_ConnectionFailed;
            redis.InternalError += Redis_InternalError;
            redis.ErrorMessage += Redis_ErrorMessage;
            oldRedis?.Close();
        }
        catch (Exception e)
        {
            Logger.Log.Error("ConnectionMultiplexer.Connect error.", e);
        }
    }

    private static void Redis_ErrorMessage(object sender, RedisErrorEventArgs e)
    {
        Logger.Log.Error("Redis_InternalError:" + e.Message);
    }

    private static void Redis_InternalError(object sender, InternalErrorEventArgs e)
    {
        Logger.Log.Error("Redis_InternalError:" + e.Exception.Message);
    }

    private static void Redis_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
    {
        Logger.Log.Error("Redis_ConnectionFailed:" + e.Exception?.Message);
    }

    private static ConnectionMultiplexer RedisMultiplexer
    {
        get
        {
            lock (multiplexerLock)
            {
                if (redis is not { IsConnected: true })
                {
                    ConnectRedis();
                }

                return redis;
            }
        }
    }

    public static IDatabase GetDatabase()
    {
        return RedisMultiplexer.GetDatabase();
    }

    public static ConfigurationOptions GetRedisConfig()
    {
        var redisConfig = new ConfigurationOptions();
        redisConfig.EndPoints.Add(AppSettingService.AppSetting.RedisConfig.EndPoint);
        redisConfig.Password = AppSettingService.AppSetting.RedisConfig.Password;
        redisConfig.DefaultDatabase = AppSettingService.AppSetting.RedisConfig.DefaultDatabase;
        redisConfig.ConnectRetry = CONNECT_RETRY;
        redisConfig.ConnectTimeout = CONNECT_TIMEOUT;
        redisConfig.SyncTimeout = SYNC_TIMEOUT;
        redisConfig.AsyncTimeout = ASYNC_TIMEOUT;
        redisConfig.AbortOnConnectFail = false;

        return redisConfig;
    }
}