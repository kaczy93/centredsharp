using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using CentrED.Server;
using Microsoft.Xna.Framework;

namespace CentrED; 

public class Program {
   
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

    private static CEDServer _server;

    private static void RunServer() {
        var pathToCedserverXml = @"C:\git\CentrEDSharp\Server\bin\Debug\net7.0\Cedserver.xml";
        new Task(() => {
            _server = new CEDServer(new[] { pathToCedserverXml });
            _server.Run();
        }).Start();
        while (_server == null || !_server.Running) {
            Thread.Sleep(1);
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        _rootDir = AppContext.BaseDirectory;
        Console.WriteLine($"Root Dir: {_rootDir}");

        _loadContext = AssemblyLoadContext.Default;
        _loadContext.ResolvingUnmanagedDll += ResolveUnmanagedDll;
        _loadContext.Resolving += ResolveAssembly;

        RunServer();
        
        using Game g = new CentrEDGame();
        g.Run();
    }
}