/********************************************************************* 
*                       
*  Filename     :      Programe.cs     
* 
*  Copyright(c) :      Zebra Technologies, 2024
*   
*  Description:      
* 
*  Author:             Zebra Technologies
* 
*  Creation Date:      2/23/2024 
* 
*  Derived From:     
* 
*  Edit History: 
*        
**********************************************************************/

using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Runtime.InteropServices;
using System.IO;
using GigaDatCreatorDLL;



namespace DatGenerator
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            //EC Levels and PID Value
            String EC_Level;
            String PID_Value;
            if (args.Length == 0)
            {
               return ;
            }
            else if (args.Length == 1)
            {
               
                AttachConsole(ATTACH_PARENT_PROCESS);
                if (args[0] == "-h" || args[0] == "-H")
                {
                    Console.WriteLine("\n\n\n\t*************************** Giga DAT Creator 2024  ***************************************");
                    Console.WriteLine("\tCommand Format :");
                    Console.WriteLine("\tGigaDatCreator.exe [Scanner model] [EC level] [PID value] [Scncfg file] [Firmware DAT file]");
                    Console.WriteLine("\n");
                    Console.WriteLine("\t ++++++++++ When Auxiliary Scanner Available ++++++++++");
                    Console.WriteLine("\tGigaDatCreator.exe [Scanner model] [EC level] [PID value] [is remove action] [Scncfg file] [Firmware DAT file] [Aux Scanner model] [EC level] [PID value] [Aux Scncfg file] [Aux Firmware DAT file]");
                    return;
                }
                else
                {
                    Console.WriteLine("\n\n\nPlease Enter \" > GigaDatCreator.exe -h \" or \" > GigaDatCreator.exe -H \" for Help");
                    return;
                }
            }

            // For single scanner available
            else if (args.Length == 6)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                bool isFileMissing = false;
                bool modelMatched = false;

                for (int index = 4; index < 6; index++)
                {
                    if (File.Exists(args[index]) == false)
                    {
                        isFileMissing = true;
                        Console.WriteLine("\n\n\tCouldnt Find Input File : " + args[index]);
                    }
                }

                string datIniPath = System.IO.Directory.GetCurrentDirectory() + "\\DAT_Server\\dat.ini";
                if (File.Exists(datIniPath))
                {
                    string[] lineOfContents = File.ReadAllLines(datIniPath);
                    foreach (var line in lineOfContents)
                    {
                        string[] tokens = line.Split(':');
                        if (tokens[0].CompareTo(args[0].ToUpper()) == 0)
                        {
                            modelMatched = true;
                        }
                    }
                }

                if (modelMatched == false)
                {
                    Console.WriteLine("\n\n\tScanner model is not supported");
                }

                if ((isFileMissing == false) && (modelMatched == true))
                {
                    DAT_Generator datGenerator = new DAT_Generator(args[0].ToUpper(), args[4], args[5], false, false);
                    
                    datGenerator.AddDevice();
                    datGenerator.ECLevel = args[2];
                    datGenerator.PIDValue =args[1];


                    if (datGenerator.createGigaDat2())

                    {
                        Console.WriteLine(datGenerator.FinalGidaDatFileName);
                        //Console.WriteLine("\n\n Successfully created the Giga DAT file");
                    }
                    else
                    {
                        Console.WriteLine("\n\nCant Create a Giga DAT File");
                    }
                }

                DAT_Generator.cleanDatServerFolder();
                DAT_Generator.RemoveTempDirectory();
                DAT_Generator.cleanUploadFolder();
                DAT_Generator.outputDirectory = "";
                return;
            }


            // For Aux scanners available
            else if (args.Length % 6 == 0)
            {
                //Console.ReadKey();
                AttachConsole(ATTACH_PARENT_PROCESS);
                bool isFileMissing = false;
                bool modelMatched = false;
                int auxModelMatchedCnt = 0;
                int totalModelMatches = 0;
                for (int index = 4; index < args.Length; index++)
                {
                    if (index % 6 == 0)
                    {
                        index += 3;
                        continue;
                    }
                    if (File.Exists(args[index]) == false)
                    {
                        isFileMissing = true;
                        Console.WriteLine("\n\n\tCouldnt Find Input File : " + args[index]);
                    }
                }
                string datIniPath = System.IO.Directory.GetCurrentDirectory() + "\\DAT_Server\\dat.ini";
                if (File.Exists(datIniPath))
                {
                    string[] lineOfContents = File.ReadAllLines(datIniPath);
                    foreach (var line in lineOfContents)
                    {
                        string[] tokens = line.Split(':');
                        for (int i = 0; i < args.Length; i += 6)
                        {
                            if (tokens[0].CompareTo(args[i].ToUpper()) == 0)
                            {
                                if (i == 0)
                                {
                                    modelMatched = true;
                                }
                                else
                                {
                                    ++auxModelMatchedCnt;
                                }
                                ++totalModelMatches;
                            }
                        }



                    }
                }

                if (modelMatched == false)
                {
                    Console.WriteLine("\n\n\tScanner model is not supported");
                }
                else if (totalModelMatches - 1 != auxModelMatchedCnt)
                {
                    Console.WriteLine("\n\n\tAuxScanner model is not supported");
                }
                else if (isFileMissing == false)
                {

                   

                    if (DAT_GEN_LIST.AddedDevicesCnt == 0)
                    {
                        var addDevice_status = DAT_GEN_LIST.AddDevice(args[0].ToUpper(), args[4], args[5], false, false);
                        switch (addDevice_status)
                        {
                            case DAT_GEN_LIST.ERROR_LIST.INVALID_SCANNER_TYPE_ERR:
                                Console.WriteLine("Primary Scanner cannot be a auxiliary type", "Giga DAT Creator");
                                return;
                            default: break;
                        }
                    }
                    if (auxModelMatchedCnt > 0)
                    {
                        for (int i = 6; i < args.Length; i += 6)
                        {
                            var addDevice_status = DAT_GEN_LIST.AddDevice(args[i].ToUpper(), args[i + 4], args[i + 5], true, false);
                            switch (addDevice_status)
                            {
                                case DAT_GEN_LIST.ERROR_LIST.DEVICE_EXIST_ERR: // This case could not be occurred
                                    Console.WriteLine("Scanner Already Exist", "Giga DAT Creator");
                                    return;
                                case DAT_GEN_LIST.ERROR_LIST.INVALID_SCANNER_TYPE_ERR:
                                    Console.WriteLine("First Scanner should be a Primary Target", "Giga DAT Creator");
                                    return;
                                case DAT_GEN_LIST.ERROR_LIST.DEVICE_TLRN_EXIST_ERR:
                                    Console.WriteLine("Primary and AUX TLRN cannot be equal", "Giga DAT Creator");
                                    return;
                                case DAT_GEN_LIST.ERROR_LIST.INCORRECT_DAT_SELECTION:
                                   Console.WriteLine("Please select correct DAT file or select correct device", "Giga DAT Creator");
                                    return;
                                default: break;
                            }
                        }
                    }
                    foreach (var datgen in DAT_GEN_LIST.DatGenerators)
                    {
                        datgen.ECLevel = args[2];
                        datgen.PIDValue = args[1];
                        if (datgen.createGigaDat2())
                        {
                           
                           // Console.WriteLine("\n\n Succesfully Created GIGA DAT files" );
                        }
                        else
                        {
                            Console.WriteLine("\n\nCant Create a Giga DAT File");
                        }
                    }
                    if (!DAT_Generator.combineGigaDATs(DAT_GEN_LIST.DatGenerators))
                    {
                        Console.WriteLine("\n\nFailure on combine Giga Dat Creation");
                    }
                    else
                    {
                        
                        Console.WriteLine(DAT_Generator.FinalSuperDatFileName);
                    }
                    DAT_Generator.cleanDatServerFolder();
                    DAT_Generator.RemoveTempDirectory();
                    DAT_Generator.cleanUploadFolder();
                    DAT_Generator.outputDirectory = "";

                }
                return;
            }
            else
            {
                //Console.ReadKey();
                AttachConsole(ATTACH_PARENT_PROCESS);
                Console.WriteLine("\n\n\nPlease Enter \" > GigaDatCreator.exe -h \" or \" > GigaDatCreator.exe -H \" for Help");
                return;
            }
        }


    }
}
