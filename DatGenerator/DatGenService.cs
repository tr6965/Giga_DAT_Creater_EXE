/********************************************************************* 
*                       
*  Filename     :      DatGenService.cs     
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

using System;
using System.IO;
using System.Xml;
using System.Diagnostics;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace GigaDatCreatorDLL
{
    public class DAT_Generator
    {
        public string ECLevel = "1400";
        public string PIDValue = "0000";
        public static string FinalSuperDatFileName = "";
        public  string FinalGidaDatFileName = "";
        private string scannerDVValue = "";
        private string scannerHCValue = "";
        private string scannerDVValue2 = "";
        private string scannerHCValue2 = "";
        private string cradleDVValue = "";
        private string cradleHCValue = "";
        private string GigaDatTLRN = "";
        private string CombineDatTLRN = "";
        private string scannerModel = "";
        private string scncfgPath = "";
        private string datFilePath = "";
        private string configPath = "";
        private string releaseDatPath = "";
        private string configDatPath = "";
        private string gigaDatPath = "";
        private string gigaDatPathCombine = "";
        private string configFileName = "";
        private string configDatFileName = "";
        private string releaseDatFileName = "";
        private string gigaDatFileName = "";
        private string gigaDatFileNameCombine = "";
        private string scannerDatNamePrefix = "";
        private string scannerDatNamePrefix2 = "";
        private string cradleDatNamePrefix = "";
        private string currentDirectory = "";
        private string tempDirectoryPath = "";
        private bool isICONDScanner = false;
        private bool gigaDatGenerated = false;
        private bool isCordlessScanner = false;
        public static string outputDirectory = "";
        private string tlrn = "";
        private string scrn = "";
        private bool m_isAuxScanner = false;
        private string outputGigaDatPath = "";
        private string outputGigaDatDirectory = "";
        private bool removeActionAttr = false;
        private int componentCountDAT = 0;
        private int componentCountConfig = 0;

        private string cradleData = null;
        private string[] scannerData;


        public string GigaDatPath { get => gigaDatPath; set => gigaDatPath = value; }
        public bool GigaDatGenerated { get => gigaDatGenerated; set => gigaDatGenerated = value; }
        public string OutputGigaDatPath { get => outputGigaDatPath; set => outputGigaDatPath = value; }
        public string OutputGigaDatDirectory { get => outputGigaDatDirectory; set => outputGigaDatDirectory = value; }
        public string gigaDatTLRN { get => GigaDatTLRN; set => GigaDatTLRN = value; }
        public string GigaDatFileName { get => gigaDatFileName; set => gigaDatFileName = value; }
        public string GigaDatFileNameCombine { get => gigaDatFileNameCombine; set => gigaDatFileNameCombine = value; }
        public string Tlrn { get => tlrn; set => tlrn = value; }
        public string ScannerModel { get => scannerModel; set => scannerModel = value; }
        public string DatFilePath { get => datFilePath; }
        public string ScncfgPath { get => scncfgPath; }
        public bool IsAuxScanner { get => m_isAuxScanner; }
        public string Scrn { get => scrn; set => scrn = value; }

        public DAT_Generator(string scanner_model, string scncfg_path, string dat_path, bool isAuxScanner, bool removeAAttr)
        {
            m_isAuxScanner = isAuxScanner;
            currentDirectory = System.IO.Directory.GetCurrentDirectory(); /*System.IO.Path.GetDirectoryName(Application.ExecutablePath);*/
            scannerModel = scanner_model;
            scncfgPath = scncfg_path;
            datFilePath = dat_path;
            configFileName = "Config_" + this.GetHashCode() + ".scncfg";
            releaseDatFileName = System.IO.Path.GetFileName(dat_path);
            configPath = currentDirectory + "\\DAT_Server\\" + configFileName;
            releaseDatPath = currentDirectory + "\\DAT_Server\\" + releaseDatFileName;
            tempDirectoryPath = currentDirectory + "\\DAT_Server\\Temp\\";
            removeActionAttr = removeAAttr;

            if (scanner_model.Contains("+") == true)
            {
                isCordlessScanner = true;
            }
            if (scanner_model.Contains("ICOND") == true)
            {
                isICONDScanner = true;
            }

            if (Directory.Exists(tempDirectoryPath))
            {
                var filesInTempDirectory = Directory.GetFiles(tempDirectoryPath);
                foreach (string file in filesInTempDirectory)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                
            }
            else
            {
                System.IO.Directory.CreateDirectory(tempDirectoryPath);
            }


            if (!m_isAuxScanner) cleanDatServerFolder();

            // copy scncfg file and dat file into DAT_Server path 
            File.SetAttributes(scncfg_path, FileAttributes.Normal);
            File.SetAttributes(dat_path, FileAttributes.Normal);
            File.Copy(scncfg_path, configPath, true);
            File.Copy(dat_path, releaseDatPath, true);
        }
        ~DAT_Generator()
        {
        }

        // Add device to device list
        public int AddDevice()
        {
            Directory.SetCurrentDirectory(currentDirectory + "\\DAT_Server\\");

            if (!GetConfigAndDatData())
            {
                return 1;
            }
            getHeaderDetails2();
            if (componentCountDAT != componentCountConfig)
            {
                return 2;
            }
            Directory.SetCurrentDirectory(currentDirectory);
            return 0;
        }

        // Create GIGA DATs
        public bool createGigaDat()
        {
            Directory.SetCurrentDirectory(currentDirectory + "\\DAT_Server\\");

            scncfgToDAT();

            if (isCordlessScanner == true)
            {
                if (isICONDScanner == false)
                {
                    combineTwoDats("ScannerConfig.dat", "CradleConfig.dat", "CombinedDat.dat", CombineDatTLRN);
                    combineTwoDats("CombinedDat.dat", releaseDatFileName, GigaDatFileNameCombine, GigaDatTLRN);
                }
                else
                {
                    combineTwoDats("ScannerConfig.dat", "CradleConfig.dat", "CombinedDat.dat", CombineDatTLRN);
                    combineTwoDats("ScannerConfig2.dat", "CombinedDat.dat", "CombinedDat2.dat", CombineDatTLRN);
                    combineTwoDats("CombinedDat2.dat", releaseDatFileName, GigaDatFileNameCombine, GigaDatTLRN);

                }
            }
            else
            {
                var x = this.configDatFileName;
                if (isICONDScanner == true)
                {
                    combineTwoDats("ScannerConfig.dat", "ScannerConfig2.dat", "CombinedDat.dat", GigaDatTLRN);
                    combineTwoDats("CombinedDat.dat", releaseDatFileName, GigaDatFileName, GigaDatTLRN);
                }
                else
                {
                    combineTwoDats("ScannerConfig.dat", releaseDatFileName, GigaDatFileName, GigaDatTLRN);
                }
            }

             copyDatsToOutputFolder();
           
            Directory.SetCurrentDirectory(currentDirectory);
 
            return GigaDatGenerated;
        }



        // Create GIGA DATs
        public bool createGigaDat2()
        {
            Directory.SetCurrentDirectory(currentDirectory + "\\DAT_Server\\");

            scncfgToDAT2();

            int i=0;

            if (isCordlessScanner == true)
            {
                  combineTwoDats("CradleConfig.dat", "ScannerConfig0.dat", "CombinedDat0.dat", CombineDatTLRN);
                for ( i = 0; i < scannerData.Length - 1; i++)
                {
                    combineTwoDats("CombinedDat" + i + ".dat", "ScannerConfig" + (i+1) + ".dat", "CombinedDat" + (i + 1) + ".dat", CombineDatTLRN);
                }
                combineTwoDats("CombinedDat" + i + ".dat", releaseDatFileName, GigaDatFileNameCombine, GigaDatTLRN);
            }
            else {
                if (scannerData.Length == 1)
                {
                    combineTwoDats("ScannerConfig0.dat", releaseDatFileName, GigaDatFileName, GigaDatTLRN);
                }
                else { 

                    combineTwoDats("ScannerConfig0.dat", "ScannerConfig1.dat", "CombinedDat0.dat", CombineDatTLRN);
                    for (i = 0; i < scannerData.Length - 2; i++)
                    {
                        combineTwoDats("CombinedDat" + i + ".dat", "ScannerConfig" + (i + 2) + ".dat", "CombinedDat" + (i + 1) + ".dat", CombineDatTLRN);
                    }
                    combineTwoDats("CombinedDat" + i + ".dat", releaseDatFileName, GigaDatFileName, GigaDatTLRN);
                }

            }
            
            copyDatsToOutputFolder();

            Directory.SetCurrentDirectory(currentDirectory);

            return GigaDatGenerated;
        }

        //Clean DAT saver folder
        public static void cleanDatServerFolder()
        {
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            string[] datFilePaths = System.IO.Directory.GetFiles(currentDirectory + "\\DAT_Server\\", "*.DAT");
            foreach (string filePath in datFilePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                System.IO.File.Delete(filePath);
            }

            string[] confFilePaths = System.IO.Directory.GetFiles(currentDirectory + "\\DAT_Server\\", "*.scncfg");
            foreach (string filePath in confFilePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                System.IO.File.Delete(filePath);
            }

            string[] dalFilePaths = System.IO.Directory.GetFiles(currentDirectory + "\\DAT_Server\\", "*.DAL");
            foreach (string filePath in dalFilePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                System.IO.File.Delete(filePath);
            }

            string[] txtFilePaths = System.IO.Directory.GetFiles(currentDirectory + "\\DAT_Server\\", "*.txt");
            foreach (string filePath in txtFilePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                System.IO.File.Delete(filePath);
            }
        }
        //Clean Upload files folder
        public static void cleanUploadFolder()
        {
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();/*System.IO.Path.GetDirectoryName(Application.ExecutablePath);*/
            string[] datFilePaths = System.IO.Directory.GetFiles(currentDirectory + "\\UploadFiles\\", "*.DAT");
            foreach (string filePath in datFilePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                System.IO.File.Delete(filePath);
            }

            string[] confFilePaths = System.IO.Directory.GetFiles(currentDirectory + "\\UploadFiles\\", "*.scncfg");
            foreach (string filePath in confFilePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                System.IO.File.Delete(filePath);
            }

         
        }
        //Remove Temp Derectory
        public static void RemoveTempDirectory()
        {

            var tempDirectory = System.IO.Directory.GetCurrentDirectory() + "\\DAT_Server\\Temp";
            if (!System.IO.Directory.Exists(tempDirectory)) return;
            DirectoryInfo dinfo = new DirectoryInfo(tempDirectory);
            var fileInfos = dinfo.GetFiles();
            if (dinfo.Exists && (fileInfos.Length != 0))
            {
                foreach (FileInfo fileInfo in fileInfos)
                {
                    File.SetAttributes(fileInfo.FullName, FileAttributes.Normal);
                }
            }
            System.IO.Directory.Delete(tempDirectory, true);
        }





            // Get header details
            private void getHeaderDetails()
        {
            string datIniPath = currentDirectory + "\\DAT_Server\\dat.ini";

            if (checkFileExist(datIniPath))
            {
                string[] lineOfContents = File.ReadAllLines(datIniPath);

                if (isCordlessScanner == true)
                {
                    foreach (var line in lineOfContents)
                    {
                        string[] tokens = line.Split(':');
                        if (scannerModel.CompareTo(tokens[0]) == 0)
                        {
                            string[] datData = tokens[1].Split(';');
                            string[] datOne = datData[0].Split(',');
                            string[] datTwo = datData[1].Split(',');
                            cradleHCValue = datOne[1];
                            cradleDVValue = (Convert.ToInt32(datOne[0], 16).ToString()).PadLeft(4, '0');
                            cradleDatNamePrefix = datOne[2];
                            scannerDVValue = (Convert.ToInt32(datTwo[0], 16).ToString()).PadLeft(4, '0');
                            scannerHCValue = datTwo[1];
                            scannerDatNamePrefix = datTwo[2];
                            if (isICONDScanner == true)
                            {
                                string[] datThree = datData[2].Split(',');
                                scannerDVValue2 = (Convert.ToInt32(datThree[0], 16).ToString()).PadLeft(4, '0');
                                scannerHCValue2 = datThree[1];
                                scannerDatNamePrefix2 = datThree[2];
                            }
                            
                            componentCountConfig = Int32.Parse(datData[3]);
                        }
                    }
                }
                else
                {
                    foreach (var line in lineOfContents)
                    {
                        string[] tokens = line.Split(':');
                        if (scannerModel.CompareTo(tokens[0]) == 0)
                        {
                            string[] datData = tokens[1].Split(';');
                            string[] datOne = datData[0].Split(',');
                            scannerHCValue = datOne[1];
                            scannerDVValue = (Convert.ToInt32(datOne[0], 16).ToString()).PadLeft(4, '0');
                            scannerDatNamePrefix = datOne[2];

                            if (isICONDScanner == true)
                            {
                                string[] datThree = datData[1].Split(',');
                                scannerDVValue2 = (Convert.ToInt32(datThree[0], 16).ToString()).PadLeft(4, '0');
                                scannerHCValue2 = datThree[1];
                                scannerDatNamePrefix2 = datThree[2];
                                componentCountConfig = Int32.Parse(datData[2]);
                            }
                            else
                            {
                                if (scannerDVValue == "2204")
                                {
                                    componentCountConfig = Int32.Parse(datData[1]) + 1;
                                }
                                else
                                {
                                    componentCountConfig = Int32.Parse(datData[1]);
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }


        // Get header details2
        private void getHeaderDetails2()
        {
            string datIniPath = currentDirectory + "\\DAT_Server\\dat.ini";

            if (checkFileExist(datIniPath))
            {
                string[] lineOfContents = File.ReadAllLines(datIniPath);


                foreach (var line in lineOfContents)
                {
                    string[] parts = line.Split(':');

                    if (scannerModel.CompareTo(parts[0]) == 0)
                    {
                        string model = parts[0];
                        string firmwareData = parts[1];

                        string[] firmwareComponents = firmwareData.Split(';');


                        int numberOfComponents = int.Parse(firmwareComponents[firmwareComponents.Length - 1]);

                 
                        if (isCordlessScanner)
                        {

                            cradleData = firmwareComponents[0];
                            scannerData = new string[numberOfComponents - 1]; // Remaining components are scanner data

                            for (int i = 1; i < numberOfComponents; i++)
                            {
                                scannerData[i - 1] = firmwareComponents[i];
                            }

                            componentCountConfig = scannerData.Length + 1;
                        }
                        else
                        {
                            // If not cordless, all components are scanner data
                            scannerData = new string[numberOfComponents];

                            for (int i = 0; i < numberOfComponents; i++)
                            {
                                scannerData[i] = firmwareComponents[i];
                            }

                            componentCountConfig = scannerData.Length;
                        }

                    }
                }

            }
        }

        //Copy DATs to Output folder
        public  void copyDatsToOutputFolder()
        {
            if (!m_isAuxScanner)
            {
                outputDirectory = currentDirectory + "\\Output\\";
                
            }

            OutputGigaDatDirectory = outputDirectory; 
            string newGigaDatFileName = 'G' + GigaDatFileName.Substring(1); 
            outputGigaDatPath = OutputGigaDatDirectory + newGigaDatFileName;
          

            if (isCordlessScanner == true)
            {
                if (checkFileExist(gigaDatPathCombine))
                {
                    File.Copy(gigaDatPathCombine, outputGigaDatPath, true);
                    FinalGidaDatFileName = newGigaDatFileName;
                    GigaDatGenerated = true;
                }
            }
            else
            {
                if (checkFileExist(gigaDatPath))
                {
                    File.Copy(gigaDatPath, outputGigaDatPath, true);
                    FinalGidaDatFileName = newGigaDatFileName;
                    GigaDatGenerated = true;
                }
            }

        }

        // Check for filess exists
        public static bool checkFileExist(params string[] pathList)
        {
            for (int i = 0; i < pathList.Length; i++)
            {
                if (!(File.Exists(pathList[i])))
                {
                    return false;
                }
            }

            return true;
        }

        // Convert Scncfg to DAT
        private void scncfgToDAT()
        {
            string destFilePath = "";

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "DAT_makeG.bat";

            if (isCordlessScanner == true)
            {
                string firmwareName;
                string outputFile;

                if (removeActionAttr == true)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(File.ReadAllText(configFileName));

                    XmlNodeList vals = doc.SelectNodes(@"scannerconfig/attrib_list/attribute");
                    foreach (XmlNode element in vals)
                    {
                        string AttribType = element.SelectSingleNode("datatype").InnerText;
                        string AttribNum = element.SelectSingleNode("id").InnerText;
                        if (AttribType.CompareTo("X") == 0)
                        {
                            string message = "Remove Action Attribute : " + AttribNum;
                            string title = "GigaDatCreator";
                            Console.WriteLine(message, title);
                            element.ParentNode.RemoveChild(element);
                        }

                    }
                    doc.Save(configFileName);
                }

                //Generate scanner dat
            
                firmwareName = scannerDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                outputFile = 'H' + firmwareName.Substring(1);
                p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + scannerDVValue + " " + scannerHCValue;
                p.Start();
                p.WaitForExit(4000);
                File.Copy(outputFile, tempDirectoryPath + "ScannerConfig.dat", true);

                if (isICONDScanner == true)
                {
                    firmwareName = scannerDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                    outputFile = 'H' + firmwareName.Substring(1);
                    p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + scannerDVValue2 + " " + scannerHCValue2;
                    p.Start();
                    p.WaitForExit(4000);
                    File.Copy(outputFile, tempDirectoryPath + "ScannerConfig2.dat", true);
                }

                //Generate cradle dat
           
                firmwareName = cradleDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                outputFile = 'H' + firmwareName.Substring(1);
                p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + cradleDVValue + " " + cradleHCValue;
                p.Start();
                p.WaitForExit(4000);
                File.Copy(outputFile, tempDirectoryPath + "CradleConfig.dat", true);

                //Copy release dat file to temp folder
                destFilePath = System.IO.Path.Combine(tempDirectoryPath, releaseDatFileName);
                System.IO.File.Copy(releaseDatPath, destFilePath, true);
                releaseDatPath = tempDirectoryPath + releaseDatFileName;
            }
            else
            {
                string firmwareName;
                string outputFile;

                if (removeActionAttr == true)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(File.ReadAllText(configFileName));

                    XmlNodeList vals = doc.SelectNodes(@"scannerconfig/attrib_list/attribute");
                    foreach (XmlNode element in vals)
                    {
                        string AttribType = element.SelectSingleNode("datatype").InnerText;
                        string AttribNum = element.SelectSingleNode("id").InnerText;
                        if (AttribType.CompareTo("X") == 0)
                        {
                            //element.ParentNode.RemoveChild(element);
                            string message = "Remove Action Attribute : " + AttribNum;
                            string title = "GigaDatCreator";
                            Console.WriteLine(message, title);
                            element.ParentNode.RemoveChild(element);
                        }

                    }
                    doc.Save(configFileName);
                }

               

                firmwareName = scannerDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                outputFile = 'H' + firmwareName.Substring(1);
                p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + scannerDVValue + " " + scannerHCValue;
                p.Start();
                p.WaitForExit(4000);
                File.Copy(outputFile, tempDirectoryPath + "ScannerConfig.dat", true);

                if (isICONDScanner == true)
                {
                    firmwareName = scannerDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                    outputFile = 'H' + firmwareName.Substring(1);
                    p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + scannerDVValue2 + " " + scannerHCValue2;
                    p.Start();
                    p.WaitForExit(4000);
                    File.Copy(outputFile, tempDirectoryPath + "ScannerConfig2.dat", true);
                }

                //copy release dat file to temp folder
                destFilePath = System.IO.Path.Combine(tempDirectoryPath, releaseDatFileName);
                System.IO.File.Copy(releaseDatPath, destFilePath, true);
                releaseDatPath = tempDirectoryPath + releaseDatFileName;
            }

            
        }


        // Convert Scncfg to DAT
        private void scncfgToDAT2()
        {

            string destFilePath = "";

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "DAT_makeG.bat";


            string firmwareName;
            string outputFile;

            if (removeActionAttr == true)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(configFileName));

                XmlNodeList vals = doc.SelectNodes(@"scannerconfig/attrib_list/attribute");
                foreach (XmlNode element in vals)
                {
                    string AttribType = element.SelectSingleNode("datatype").InnerText;
                    string AttribNum = element.SelectSingleNode("id").InnerText;
                    if (AttribType.CompareTo("X") == 0)
                    {
                        //element.ParentNode.RemoveChild(element);
                        string message = "Remove Action Attribute : " + AttribNum;
                        string title = "GigaDatCreator";
                        Console.WriteLine(message, title);
                        element.ParentNode.RemoveChild(element);
                    }

                }
                doc.Save(configFileName);
            }

            for (int i = 0; i < scannerData.Length; i++)
            {

                string[] parts_pre = scannerData[i].Split(',');
                scannerDatNamePrefix = parts_pre[parts_pre.Length - 1];
                firmwareName = scannerDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                outputFile = 'H' + firmwareName.Substring(1);

                string[] parts_scannerdata = scannerData[i].Split(',');
                string DVvalue = (Convert.ToInt32(parts_scannerdata[0], 16).ToString()).PadLeft(4, '0');
                string HCvalue = parts_scannerdata[1];

                p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + DVvalue + " " + HCvalue;
                p.Start();
                p.WaitForExit(4000);
                File.Copy(outputFile, tempDirectoryPath + "ScannerConfig" + i + ".dat", true);
            }

            if (isCordlessScanner == true)
            {
                string[] parts_pre = cradleData.Split(',');
                scannerDatNamePrefix = parts_pre[parts_pre.Length - 1];
                firmwareName = scannerDatNamePrefix + Tlrn.Substring(6) + "D0.DAT";
                outputFile = 'H' + firmwareName.Substring(1);

                string[] parts_scannerdata = cradleData.Split(',');
                string DVvalue = (Convert.ToInt32(parts_scannerdata[0], 16).ToString()).PadLeft(4, '0');
                string HCvalue = parts_scannerdata[1];

                p.StartInfo.Arguments = " " + configFileName + " " + firmwareName + " " + ECLevel + " " + DVvalue + " " + HCvalue;
                p.Start();
                p.WaitForExit(4000);
                File.Copy(outputFile, tempDirectoryPath + "CradleConfig.dat", true);

            }
            //copy release dat file to temp folder
            destFilePath = System.IO.Path.Combine(tempDirectoryPath, releaseDatFileName);
            System.IO.File.Copy(releaseDatPath, destFilePath, true);
            releaseDatPath = tempDirectoryPath + releaseDatFileName;

        }

            //Combine two dat files inside temp folder
            private void combineTwoDats(string dat1, string dat2, string outputName, string tlrn)
        {
            string datPath1 = currentDirectory + "\\DAT_Server\\Temp\\" + dat1;
            string datPath2 = currentDirectory + "\\DAT_Server\\Temp\\" + dat2;

            if (!checkFileExist(datPath1, datPath2))
            {
                return;
            }

            string datFile1 = "Temp\\" + dat1;
            string datFile2 = "Temp\\" + dat2;

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "DAT-Combiner.exe";
            p.StartInfo.Arguments = "-H " + PIDValue + " " + ECLevel + " " + tlrn + " " + " -S " + datFile1 + " -S " + datFile2 + " -O " + outputName;
            p.Start();
            p.WaitForExit(4000);

            //Copy output file to temp folder
            string outputFilePath = currentDirectory + "\\DAT_Server\\" + outputName;
            string destFilePath = currentDirectory + "\\DAT_Server\\Temp\\" + outputName;

            if (checkFileExist(outputFilePath))
            {
                System.IO.File.Copy(outputFilePath, destFilePath, true);
                }
        }


        //Combine two dat files inside temp folder
        public static bool combineGigaDATs(List<DAT_Generator> DatGenerators)
        {
            if (DatGenerators.Count < 2)
            {
                return false;
            }
            if (!checkFileExist(DatGenerators[0].OutputGigaDatPath))
            {
                return false;
            }
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            var outputDirectory = DAT_Generator.outputDirectory; // "CombinedGigaDATs\\";
            var primaryDAT_TLRN = DatGenerators[0].gigaDatTLRN;
            string gigaDATCombinedTLRN = "J" + primaryDAT_TLRN.Remove(0, 1);
            string combinedGigaDATOutputFilePath = outputDirectory + gigaDATCombinedTLRN + ".DAT";

            /* combination limited to  max of int 2^32-1*/
            //var totalCombination = (1 << DatGenerators.Count) - 1;
            //List<List<DAT_Generator>> totalCombination_List = new List<List<DAT_Generator>>(totalCombination > 0 ? totalCombination:0);
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = currentDirectory + "\\DAT_Server\\DAT-Combiner.exe";
            p.StartInfo.Arguments = "-H " + DatGenerators[0].PIDValue + " " + DatGenerators[0].ECLevel + " " + gigaDATCombinedTLRN + " " + " -S " +
               DatGenerators[0].OutputGigaDatPath;
            bool success = true;
            for (int i = 1; i < DatGenerators.Count; ++i)
            {

                if (!checkFileExist(DatGenerators[i].OutputGigaDatPath))
                {
                    success = false;
                    break;
                }
                p.StartInfo.Arguments += " -S " + DatGenerators[i].OutputGigaDatPath;

            }

            if (!success)
            {
                return false;
            }
            System.IO.Directory.CreateDirectory(outputDirectory);
            p.StartInfo.Arguments += " -O " + combinedGigaDATOutputFilePath;
            p.Start();
            p.WaitForExit(4000);



            if (checkFileExist(combinedGigaDATOutputFilePath))
            {
                FinalSuperDatFileName= gigaDATCombinedTLRN + ".DAT";
                DeleteGigaDats(outputDirectory);
                return true;
            }
            return false;
        }

        //Get config and DAT data
        private bool GetConfigAndDatData()
        {
            Boolean setPID = false;
            Boolean setECLevel = false;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(configPath));

            XmlNodeList vals = doc.SelectNodes(@"scannerconfig/attrib_list/attribute");
            foreach (XmlNode element in vals)
            {
                string AttribID = element.SelectSingleNode("id").InnerText;
                if (AttribID.CompareTo("1725") == 0)
                {
                    string param1725 = element.SelectSingleNode("value").InnerText;

                    if (param1725.CompareTo("0") != 0)
                    {
                        int x = Convert.ToInt32(param1725);
                        PIDValue = x.ToString().PadLeft(4, '0');
                        setPID = true;
                    }
                }
                if (AttribID.CompareTo("1710") == 0)
                {
                    string param1710 = element.SelectSingleNode("value").InnerText;
                    int y = Convert.ToInt32(param1710);
                    ECLevel = y.ToString().PadLeft(4, '0');
                    setECLevel = true;
                }
            }

            // Execute the Command to get DAT file header details
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "DAT-Reader.exe";
            p.StartInfo.Arguments = " " + "\"" + releaseDatFileName + "\"";
            p.Start();
            string output = p.StandardOutput.ReadToEnd(); //check output has failed string for success or fail
            p.WaitForExit(4000);

            string[] tempS1 = output.Split(new string[] { "SC[0]:" }, StringSplitOptions.None);
            string[] tempS2 = tempS1[0].Split(new string[] { "EcLevel=" }, StringSplitOptions.None);
            string[] tempS3 = tempS2[0].Split(new string[] { "PID=" }, StringSplitOptions.None);

            componentCountDAT = Regex.Matches(output, "SC:0x0003/3").Count;

            // Since SCRN AND TLRN are mendatory parameters of a DAT, no need of chekcing of exisence
            // Also TLRN at the end of the header
            string TLRN = (tempS1[1].Split(new string[] { "TLRN:", "\r" }, StringSplitOptions.RemoveEmptyEntries))[1];
            
            if (TLRN.Contains("---"))
            {
                // go to SCRN
                // if(SCRN.Contains("---"))
                // {
                //return false;
                // }
                //return false;
            }
            Tlrn = TLRN;

            releaseDatFileName = Tlrn + "D0.DAT";
           
            char[] temp4 = TLRN.ToCharArray();
            temp4[0] = 'C';
            GigaDatTLRN = new string(temp4);
            temp4[0] = 'S';
            CombineDatTLRN = new string(temp4);
            GigaDatFileName = GigaDatTLRN + "D0.DAT";
            GigaDatFileNameCombine = CombineDatTLRN + "D0.DAT";
            gigaDatPath = currentDirectory + "\\DAT_Server\\" + GigaDatFileName;
            gigaDatPathCombine = currentDirectory + "\\DAT_Server\\" + GigaDatFileNameCombine;
            string tXScannerECLevel = (tempS2[1].Replace("\r\n", string.Empty)).PadLeft(4, '0');
            string tXScannerDVLevel = (tempS3[1].Replace("\r\n", string.Empty)).PadLeft(4, '0');
            tXScannerECLevel = tXScannerECLevel.Replace(" ", string.Empty);
            tXScannerDVLevel = tXScannerDVLevel.Replace(" ", string.Empty);

            if (setECLevel == false)
            {
                int x = Int32.Parse((tXScannerECLevel.Substring(2, 4)));
                x = x + 1;

                ECLevel = x.ToString().PadLeft(4, '0');
                setECLevel = true;
            }
            if (setPID == false)
            {
                PIDValue = tXScannerDVLevel.Substring(2, 4);
                setPID = true;
            }
            return true;
        }

        //Delete Giga Dats (*)
        public static void DeleteGigaDats(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                string[] files = Directory.GetFiles(dirPath, "G*.dat");
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"{dirPath} is not a directory.");
            }
        }

    }

    /*
     * Only Sinlge LIST OF DATGENRATORS will be maintained,
     * hence static class is enough to hold collection of them in globally.
     * */
    public static class DAT_GEN_LIST
    {
        public enum ERROR_LIST { NO_ERR, DEVICE_EXIST_ERR, INVALID_SCANNER_TYPE_ERR, DEVICE_TLRN_EXIST_ERR, INCORRECT_DAT_SELECTION, INVALID_TLRN_ERR };
        private static int m_addedDevicesCnt = 0;
        private static List<DAT_Generator> m_datGenerators = new List<DAT_Generator>();

        public static int AddedDevicesCnt { get => m_addedDevicesCnt; }
        public static List<DAT_Generator> DatGenerators { get => m_datGenerators; }

        public static ERROR_LIST AddDevice(string scanner_model, string scncfg_path, string dat_path, bool isAuxScanner, bool removeActionAtt)
        {
            if (m_addedDevicesCnt > 0) // after adding primiary scanner
            {
                for (int i = 0; i < m_addedDevicesCnt; ++i)
                {
                    if (scanner_model == m_datGenerators[i].ScannerModel ||
                    (dat_path == m_datGenerators[i].DatFilePath && scncfg_path == m_datGenerators[i].ScncfgPath))
                    {
                        return ERROR_LIST.DEVICE_EXIST_ERR;
                    }
                }

            }
            else
            {
                // Since only a single primary scanner suppose to be added ; therefore it should be verified
                if (isAuxScanner != false)
                {
                    return ERROR_LIST.INVALID_SCANNER_TYPE_ERR;
                }
            }
            DAT_Generator datGenInstance = new DAT_Generator(scanner_model, scncfg_path, dat_path, isAuxScanner, removeActionAtt);
    
            int resultAddDev = datGenInstance.AddDevice();
            if (resultAddDev == 1)
            {
                return ERROR_LIST.INVALID_TLRN_ERR;
            }
            if (resultAddDev == 2)
            {
                return ERROR_LIST.INCORRECT_DAT_SELECTION;
            }
            if (m_addedDevicesCnt > 0 && (datGenInstance.Tlrn == m_datGenerators[0].Tlrn))
            {
                return ERROR_LIST.DEVICE_TLRN_EXIST_ERR;
            }
            m_datGenerators.Add(datGenInstance);
            ++m_addedDevicesCnt;
            return ERROR_LIST.NO_ERR;
        }

        public static void RemoveDevice(int index)
        {
            m_datGenerators.RemoveAt(index);
        }
        public static void RemoveAllDevices()
        {
            m_datGenerators.Clear();
            m_addedDevicesCnt = 0;
        }
    }
}
