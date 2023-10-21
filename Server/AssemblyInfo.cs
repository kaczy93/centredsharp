using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion(CentrED.Server.Constants.Assembly.Version)]
[assembly: AssemblyFileVersion(CentrED.Server.Constants.Assembly.Version)]

[assembly: AssemblyTitle(CentrED.Server.Constants.Assembly.Title)]
[assembly: AssemblyDescription(CentrED.Server.Constants.Assembly.Description)]

[assembly: AssemblyCompany(CentrED.Server.Constants.Assembly.Company)]
[assembly: AssemblyProduct(CentrED.Server.Constants.Assembly.Product)]

[assembly: AssemblyCopyright(CentrED.Server.Constants.Assembly.Copyright)]
[assembly: AssemblyTrademark(CentrED.Server.Constants.Assembly.Trademark)]

[assembly: AssemblyCulture(CentrED.Server.Constants.Assembly.Culture)]
[assembly: AssemblyConfiguration(CentrED.Server.Constants.Assembly.Config)]

[assembly: ComVisible(false)]
[assembly: Guid("beafea65-f3d3-446c-b944-8d4445e735c4")]

namespace CentrED.Server
{
    public static class Constants
    {
        public const string Website = "https://kaczy93.github.io/centredsharp/";

        public static class Assembly
        {
            public const string Version = "0.1.0.0";

            public const string Title = "Cedserver";
            public const string Description = "CentrED Server";

            public const string Company = "Nelderim";
            public const string Product = "Cedserver#";

            public const string Copyright = "2023 Kaczy";
            public const string Trademark = "";

            public const string Culture = "";
            public const string Config = "";
        }
    }
}