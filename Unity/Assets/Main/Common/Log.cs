using System;

public static class Log
{
    public static void Trace(object msg)
    {
        Trace("{0}", msg);
    }

    
    public static void Debug(object msg)
    {
        Debug("{0}", msg);
    }

    public static void Info(object msg)
    {        
        Info("{0}", msg);
    }


    public static void Warning(object msg)
    {
        Warning("{0}", msg);
    }


    public static void Error(object msg)
    {
        Error("{0}", msg);
    }


    public static void Fatal(object msg)
    {
        Fatal("{0}", msg);
    }

    public static void Trace(string message, params object[] args)
    {
        UnityEngine.Debug.LogFormat(message, args);
    }

    public static void Warning(string message, params object[] args)
    {
        UnityEngine.Debug.LogWarningFormat(message, args);
    }

    public static void Info(string message, params object[] args)
    {
        UnityEngine.Debug.LogFormat(message, args);
    }

    public static void Debug(string message, params object[] args)
    {
        UnityEngine.Debug.LogFormat(message, args);
    }

    public static void Error(string message, params object[] args)
    {
        UnityEngine.Debug.LogErrorFormat(message, args);
    }

    public static void Fatal(string message, params object[] args)
    {
        UnityEngine.Debug.LogAssertionFormat(message, args);
    }
}