using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Xml;

namespace giderosman
{
    class Program
    {

        static void Export(string gdrexportPath, string gprojPath, string outputPath, string packageName)
        {
            Console.WriteLine("Exporting ... ");

            if (Directory.Exists(outputPath))
               Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = gdrexportPath;
            startInfo.Arguments = "-platform android -package \""+ packageName +"\" -encrypt \"" + gprojPath + "\" \"" + outputPath + "\"";
            Console.WriteLine(startInfo.Arguments);
            int exitCode;
            using (Process proc = Process.Start(startInfo))
            {
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }

            Console.WriteLine("Exporting finish : " + exitCode);
        }

        //membuat salinan yang memisahkan antara 2x dengan normal
        static void Separate(string inputPath, string outputPath)
        {
            string normalPath = Path.Combine(outputPath, "normal");
            string hdPath = Path.Combine(outputPath, "2x");
            
            if (Directory.Exists(normalPath))
                Directory.Delete(normalPath, true);
            if (Directory.Exists(hdPath))
                Directory.Delete(hdPath, true);            
            Directory.CreateDirectory(normalPath);
            Directory.CreateDirectory(hdPath);

            Stack<string> q = new Stack<string>();
            Dictionary<string, string> normald = new Dictionary<string, string>();
            Dictionary<string, string> highd = new Dictionary<string, string>();
            normald[Path.GetFullPath(inputPath)] = normalPath;
            highd[Path.GetFullPath(inputPath)] = hdPath;
            foreach (var files in Directory.GetFiles(inputPath))
                q.Push(Path.GetFullPath(files));
            foreach (var folders in Directory.GetDirectories(inputPath))
                q.Push(Path.GetFullPath(folders));

            while (q.Count != 0)
            {
                string path = q.Pop();
                string parent = Directory.GetParent(path).FullName;
                Console.WriteLine("processing " + path);
                if (File.Exists(path)) // it's a file
                {
                    if (!Path.GetFileName(path).Contains("@2x"))
                    {
                        normald[path] = Path.Combine(normald[parent], Path.GetFileName(path));
                        File.Copy(path, normald[path]);
                        string ext = Path.GetExtension(path);
                        if (ext == ".png" || ext == ".jpg")
                        {
                            string path2x = Path.Combine(parent, Path.GetFileNameWithoutExtension(path) + "@2x" + ext);
                            if (!File.Exists(path2x)) // jika ada gambar versi yang lebih bagus, jangan kopi file ini ke high
                            {
                                highd[path] = Path.Combine(highd[parent], Path.GetFileName(path));
                                File.Copy(path, highd[path]);
                            }
                        }
                        else
                        {
                            highd[path] = Path.Combine(highd[parent], Path.GetFileName(path));
                            File.Copy(path, highd[path]);
                        }
                    }
                    else
                    {
                        highd[path] = Path.Combine(highd[parent], Path.GetFileName(path));
                        File.Copy(path, highd[path]);
                    }
                }
                else // it's a dir
                {
                    normald[path] = Path.Combine(normald[parent], Path.GetFileName(path));
                    highd[path] = Path.Combine(highd[parent], Path.GetFileName(path));

                    Directory.CreateDirectory(normald[path]);
                    Directory.CreateDirectory(highd[path]);
                    foreach (var files in Directory.GetFiles(path))
                        q.Push(Path.GetFullPath(files));
                    foreach (var folders in Directory.GetDirectories(path))
                        q.Push(Path.GetFullPath(folders));
                }
            }
        }

        static void EditAndroidManifest(string projectPath, string versionCode, string versionName)
        {
            XmlDocument doc = new XmlDocument();
            //search android manifest
            string[] ampath = Directory.GetFiles(projectPath, "AndroidManifest.xml", SearchOption.AllDirectories);
            doc.Load(ampath[0]);
            string strNamespace = "http://schemas.android.com/apk/res/android";
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            //Console.WriteLine("aaa : " + doc.DocumentElement.NamespaceURI);
            nsmgr.AddNamespace("android", strNamespace);

            //set versionCode and versionName 
            doc.SelectSingleNode("//manifest").Attributes["android:versionCode"].InnerText = versionCode;
            doc.SelectSingleNode("//manifest").Attributes["android:versionName"].InnerText = versionName;
            
            //set installLocation to preferExternal
            doc.SelectSingleNode("//manifest").Attributes["android:installLocation"].InnerText = "preferExternal";
            
            //delete node permission for ACCESS_FINE_LOCATION
            var locationPermission = doc.SelectSingleNode("//uses-permission[@android:name='android.permission.ACCESS_FINE_LOCATION']", nsmgr);
            if(locationPermission != null)
                doc.DocumentElement.RemoveChild(locationPermission);            

            //add node permission for BILLING if it does not exist
            var checkBilling = doc.SelectSingleNode("//uses-permission[@android:name='com.android.vending.BILLING']", nsmgr);            
            if (checkBilling == null)
            {
                var billingNode = doc.CreateElement("uses-permission");
                billingNode.SetAttribute("name", strNamespace, "com.android.vending.BILLING");                            
                doc.SelectSingleNode("//manifest").InsertAfter(billingNode, doc.SelectSingleNode("//uses-sdk"));
            }                       

            //set allowBackup to false
            var allowBackupAttr = doc.CreateAttribute("android","allowBackup", strNamespace);
            allowBackupAttr.Value = "false";
            doc.SelectSingleNode("//application").Attributes.Append(allowBackupAttr);

            doc.Save(ampath[0]);
        }

        static void ReplaceIcons(string projectPath, string iconPath)
        {
            //replace icon
            string hdpiFolderPath = Directory.GetDirectories(projectPath, "drawable-hdpi", SearchOption.AllDirectories)[0];
            string mdpiFolderPath = Directory.GetDirectories(projectPath, "drawable-mdpi", SearchOption.AllDirectories)[0];
            string xhdpiFolderPath = Directory.GetDirectories(projectPath, "drawable-xhdpi", SearchOption.AllDirectories)[0];
            //xxhdpi folder does not always exist
            var check_exist = Directory.GetDirectories(projectPath, "drawable-xxhdpi", SearchOption.AllDirectories);
            string xxhdpiFolderPath = null;
            if (check_exist != null)
            {
                xxhdpiFolderPath = Path.Combine(Directory.GetDirectories(projectPath, "res", SearchOption.AllDirectories)[0], "drawable-xxhdpi");
                Directory.CreateDirectory(xxhdpiFolderPath);
            }
            else
            {
                xxhdpiFolderPath = Directory.GetDirectories(projectPath, "drawable-xxhdpi", SearchOption.AllDirectories)[0];
            }
                
            File.Copy(Path.Combine(iconPath, "hdpi", "icon.png"), Path.Combine(hdpiFolderPath, "icon.png"), true);
            File.Copy(Path.Combine(iconPath, "mdpi", "icon.png"), Path.Combine(mdpiFolderPath, "icon.png"), true);
            File.Copy(Path.Combine(iconPath, "xhdpi", "icon.png"), Path.Combine(xhdpiFolderPath, "icon.png"), true);
            File.Copy(Path.Combine(iconPath, "xxhdpi", "icon.png"), Path.Combine(xxhdpiFolderPath, "icon.png"), true);            
        }

        static void BuildApk(string projectPath, string batFilePath, string projectName, string keystorePath, string passKeystorePath, string keystoreAlias)
        {
            int exitCode = 0;            
            string unsignedApkPath = Path.Combine(projectPath, "bin", projectName + "-release-unsigned.apk");
            string signedApkPath = Path.Combine(projectPath, "bin", projectName + "-release-signed.apk");
            string signedAlignedApkPath = Path.Combine(projectPath, "bin", projectName + "-release-signed-aligned.apk");
            string antBuildPath = Path.Combine(projectPath, "build.xml");

            //copy gideros.jar(gak perlu overwrite)
            if(!File.Exists(Path.Combine(projectPath, "libs", "gideros.jar")))
                File.Copy(Path.Combine(projectPath, "gideros.jar"), Path.Combine(projectPath, "libs", "gideros.jar"));

            Console.WriteLine("Building release version apk ... ");            

            exitCode = RunProcess("android", "update project -n \"" + projectName + "\"" + " -p \"" + projectPath + "\"");            
            Console.WriteLine("android update project finish : " + exitCode);
            exitCode = RunProcess("CMD.exe", "/C ant -buildfile \"" + antBuildPath + "\" release");
            Console.WriteLine("ant release finish : " + exitCode);
            exitCode = RunProcess("CMD.exe", "/C jarsigner -sigalg SHA1withRSA -digestalg SHA1 -keystore \"" + keystorePath + "\" -signedjar \"" + signedApkPath + "\" \"" + unsignedApkPath + "\" " + keystoreAlias + " < " + "\"" + passKeystorePath+"\"");
            Console.WriteLine("signing finish : " + exitCode);
            exitCode = RunProcess("zipalign", "-f 4 \"" + signedApkPath + "\" \"" + signedAlignedApkPath + "\"");
            Console.WriteLine("aligning finish : " + exitCode);            
        }

        static int RunProcess(string programPath, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = programPath;
            startInfo.Arguments = arguments;            
            Console.WriteLine(programPath + " " + startInfo.Arguments);
            int exitCode;

            Process process = Process.Start(startInfo);            
      
            process.WaitForExit();
            exitCode = process.ExitCode;
            process.Dispose();
            return exitCode;
        }

        static void Main(string[] args)
        {
            string packageName = "com.orgcomgames.elementalclash";
            string gdrexportPath = @"C:\Program Files (x86)\Gideros\Tools\gdrexport.exe";
            string gprojPath = @"C:\Users\Asus\Desktop\Elemental Clash\EC - 2\trunk\Project\ElementalClash\ElementalClash.gproj";
            string versionCode = "6";
            string versionName = "1.0.5";
            string iconPath = @"C:\Users\Asus\Desktop\Elemental Clash\icons";
            string buildBatPath = @"C:\Users\Asus\Desktop\Elemental Clash\build.bat";
            string projectName = "ElementalClash"; // must be equal to filename gproj
            string keystorePath = @"C:\Users\Asus\Dropbox\Elemental Clash\ec_keystore";
            string passKeyStorePath = @"C:\Users\Asus\Desktop\Elemental Clash\keystore_pass.txt";
            string keystoreAlias = "elemental_clash";
                              
            string origPath = Path.GetFullPath(".\\orig");

            Console.WriteLine("giderosman");

            Export(gdrexportPath, gprojPath, origPath, packageName);            
            EditAndroidManifest(origPath, versionCode, versionName);
            ReplaceIcons(origPath, iconPath);
            //Separate(origPath, ".");
            BuildApk(Path.Combine(origPath, projectName), buildBatPath, projectName, keystorePath, passKeyStorePath, keystoreAlias);

            Console.WriteLine("DONE! press enter to exit");
#if DEBUG
            Console.Read();
#endif
        }
    }
}
