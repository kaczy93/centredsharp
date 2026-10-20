namespace CentrED;

public class CentrED
{
    public static CentrEDGame App { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        using (App = new CentrEDGame())
        {
            try
            {
                App.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                File.WriteAllText("Crash.log", e.ToString());
            }
        }
    }
}