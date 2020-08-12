using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Folder2Epub;

namespace DocXFolderToEpub
{
    public class Program
    {
        //Need to run "msbuild /t:ILMerge" in VS CMD Prompt for integrated executable.
        static void Main(string[] args)
        {    

            Console.WriteLine("Batch DocX to epub converter thrown together by Kiradien.");
            Console.WriteLine("This application will convert all docx files in a specified folder into a single epub. Folder is specified through json settings.");
            Console.WriteLine();
            Console.WriteLine($"Json Settings file can be passed as commandline argument, {Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)}_settings.json by default.");
            Console.WriteLine("If the specified or default json file does not exist, it will be created when first run, defaulting to the current directory.");
            Console.WriteLine();
            Console.WriteLine("Please make any desired changes to the settings json file.");
            Console.WriteLine("Please direct any questions to Kiradien@gmail.com or @Kiradien#0001");
            Console.WriteLine("--------------------");
            Console.WriteLine();
            try
            {
                string settingsPath = $"{Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)}_settings.json";
                if (args.Length > 0 && args[0].EndsWith("json"))
                {
                    settingsPath = args[0];
                }
                DocX2Epub docX2Epub = new DocX2Epub(settingsPath);
                docX2Epub.OnUpdate += (status) => Console.WriteLine(status);

                docX2Epub.GenerateEpub();

                Console.WriteLine("Please press any key to continue");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("-----------------------");
                Console.WriteLine(e.Message);
                Console.WriteLine("-----------------------");
                Console.WriteLine("An unhandled exception occurred while generating your epub.");
                Console.ReadKey();
            }
        }



    }
}
