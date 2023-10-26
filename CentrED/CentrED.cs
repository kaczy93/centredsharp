using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using CentrED.Client;
using CentrED.Server;
using Microsoft.Xna.Framework;

namespace CentrED; 

public class CentrED {
   
    static private AssemblyLoadContext _loadContext;
    static private string? _rootDir;

    static private Assembly? LoadFromResource(string resourceName)
    {
        Console.WriteLine($"Loading resource {resourceName}");

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = $"{assembly.GetName().Name}.{resourceName}.dll";
            if (name.StartsWith("System."))
                continue;

            using Stream? s = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}.dll");

            if (s == null || s.Length == 0)
                continue;

            return _loadContext.LoadFromStream(s);
        }

        return null;
    }

    static private Assembly? ResolveAssembly(AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
        Console.WriteLine($"Resolving assembly {assemblyName}");

        if (loadContext != _loadContext)
        {
            throw new Exception("Mismatched load contexts!");
        }

        if (assemblyName == null || assemblyName.Name == null)
        {
            throw new Exception("Unable to load null assembly");
        }

        /* Wasn't in same directory. Try to load it as a resource. */
        return LoadFromResource(assemblyName.Name);
    }

    static private IntPtr ResolveUnmanagedDll(Assembly assembly, string unmanagedDllName)
    {
        Console.WriteLine($"Loading unmanaged DLL {unmanagedDllName} for {assembly.GetName().Name}");

        /* Try the correct native libs directory first */
        string osDir = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            osDir = "x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osDir = "osx";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            osDir = "lib64";
        }

        var libraryPath = Path.Combine(_rootDir, osDir, unmanagedDllName);

        Console.WriteLine($"Resolved DLL to {libraryPath}");

        if (File.Exists(libraryPath))
            return NativeLibrary.Load(libraryPath);

        return IntPtr.Zero;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDllDirectory(string lpPathName);

    public static CEDServer? Server;
    public static readonly CentrEDClient Client = new();
    
    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        _rootDir = AppContext.BaseDirectory;
        Console.WriteLine($"Root Dir: {_rootDir}");

        _loadContext = AssemblyLoadContext.Default;
        _loadContext.ResolvingUnmanagedDll += ResolveUnmanagedDll;
        _loadContext.Resolving += ResolveAssembly;
        
        using Game g = new CentrEDGame();
        try {
            g.Run();
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
            File.WriteAllText("Crash.log", e.ToString());
        }
    }
}