using System;

namespace TestZebra
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string dllPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Interop.CoreScanner.dll");
                if (!System.IO.File.Exists(dllPath))
                {
                    Console.WriteLine("DLL NOT FOUND at " + dllPath);
                    // Try parent paths
                    dllPath = @"c:\Users\ASUS\.gemini\antigravity\scratch\VehicleVisionOCR\apps\backend-dotnet\bin\Debug\net10.0\Interop.CoreScanner.dll";
                }

                System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFrom(dllPath);
                Type scannerType = asm.GetType("CoreScanner.CCoreScannerClass") ?? asm.GetType("Interop.CoreScanner.CCoreScannerClass");
                object coreScanner = Activator.CreateInstance(scannerType);

                short[] scannerTypes = { 1, 2, 3, 6, 7, 8, 9, 11 };
                int status = 0;
                
                dynamic dynScanner = coreScanner;
                Console.WriteLine("Calling Open...");
                dynScanner.Open(0, scannerTypes, (short)scannerTypes.Length, out status);
                Console.WriteLine("Open Status: " + status);

                short numberOfScanners = 0;
                Array scannerList = null;
                string outXML = "";

                Console.WriteLine("Calling GetScanners...");
                dynScanner.GetScanners(out numberOfScanners, out scannerList, out outXML, out status);
                
                Console.WriteLine("GetScanners Status: " + status);
                Console.WriteLine("Number of Scanners: " + numberOfScanners);
                Console.WriteLine("XML Output: " + outXML);
                
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}
