using System.Diagnostics;
using System.Reflection;

namespace EnhancedBeliefs;

internal sealed class EnhancedBeliefsMod : Mod
{
#pragma warning disable CS8618 // Set by constructor
    internal static EnhancedBeliefsMod instance;
#pragma warning restore CS8618
    public EnhancedBeliefsMod(ModContentPack content) : base(content)
    {
        instance = this;

        var harmony = new Harmony(content.PackageId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Message("Enhanced Beliefs (Updated) is now initialized!");
    }

    public static Settings Settings => instance.GetSettings<Settings>();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

#if DEBUG
    public override string SettingsCategory()
    {
        return Content.Name;
    }
#endif

    public static void Message(string msg)
    {
        Log.Message("[Enhanced Beliefs (Updated)] " + msg);
    }

    public static void DevMessage(string msg)
    {
        if (Prefs.DevMode)
        {
            Log.Message($"[Enhanced Beliefs (Updated)][DEV] " + msg);
        }
    }

    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        Log.ResetMessageCount();
        DevMessage(message);
    }

    [Conditional("DEBUG")]
    public static void DebugIf(bool condition, string message)
    {
        Log.ResetMessageCount();
        if (condition)
        {
            DevMessage(message);
        }
    }

    public static void Warning(string msg)
    {
        Log.Warning("[Enhanced Beliefs (Updated)] " + msg);
    }

    public static void WarningOnce(string msg, int key)
    {
        Log.WarningOnce("[Enhanced Beliefs (Updated)] " + msg, key);
    }

    public static void Error(string msg)
    {
        Log.Error("[Enhanced Beliefs (Updated)] " + msg);
    }

    public static void ErrorOnce(string msg, int key)
    {
        Log.ErrorOnce("[Enhanced Beliefs (Updated)] " + msg, key);
    }

    public static void Exception(string msg, Exception? e = null)
    {
        Message(msg);
        if (e != null)
        {
            Log.Error(e.ToString());
        }
    }
}
