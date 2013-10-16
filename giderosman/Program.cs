using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace giderosman
{
    class Program
    {        

        static void Main(string[] args)
        {
            string inputFolderName = "test-folder";
            string outputFolderName = "export";            

            Console.WriteLine("giderosman");

            string inputPath = Path.Combine(".", inputFolderName);
            string outputPath = Path.Combine(".", outputFolderName);
            string normalPath = Path.Combine(outputPath, "normal");
            string hdPath = Path.Combine(outputPath, "2x");
            if(Directory.Exists(normalPath))
                Directory.Delete(normalPath, true);
            if (Directory.Exists(hdPath))
                Directory.Delete(hdPath, true);
            Directory.CreateDirectory(outputPath);
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
                            if (!File.Exists(path2x)) // jika ada gambar versi yang lebih bagus, jangan kopi file in ke high
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

#if DEBUG
            Console.Read();
#endif
        }
    }
}
