using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace jsreport.Local
{
    /// <summary>
    /// Direct rendering inside server-less local jsreport
    /// </summary>
    public class LocalReportingService : ILocalReportingService
    {
        private bool _isInitialized;

        /// <summary>
        ///     Relative path (from bin) to directory where the jsreport server should be exreacted  and where it should run
        ///     You want to use something like ../App_Data for web applications and just null for other types of applications
        ///     where jsreport can stay in bin folder
        /// </summary>
        public string RelativePathToServer { get; set; }

        /// <summary>
        ///     Takes precedence over RelativePathToServer and specifies directory where jsreport server should be extracted and run
        /// </summary>
        public string AbsolutePathToServer { get; set; }

        /// <summary>
        /// Directory of jsreport.Local.dll
        /// </summary>
        public string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
                return Path.GetDirectoryName(codeBase);
            }
        }

        /// <summary>
        /// Decompress the jsreport.zip and copy server.js and prod.config.json from jsreport/production folder
        /// </summary>
        public void Initialize()
        {
            if (RelativePathToServer == null)
            {
                if (IsWebApp(AppDomain.CurrentDomain))
                {
                    RelativePathToServer = Path.Combine("../App_Data/jsreport", "app");
                }
                else
                {
                    RelativePathToServer = Path.Combine("jsreport", "app");
                }
            }

            if (AbsolutePathToServer == null)
            {
                AbsolutePathToServer = Path.Combine(AssemblyDirectory, RelativePathToServer);
            }

            if (!Directory.Exists(AbsolutePathToServer))
            {
                Directory.CreateDirectory(AbsolutePathToServer);
            }

            Decompress();
            _isInitialized = true;
        }


        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template name can be taken from jsreport studio or from folder name in VS</param>
        /// <param name="data">any json serializable object</param>
        public Report Render(string templateName, object data)
        {
            return Render(new
                {
                    template = new
                        {
                            name = templateName
                        },
                    data = data
                });
        }

        /// <summary>
        /// Complex rendering, see the 
        /// </summary>
        /// <param name="request">Complex dynamic rendering request. See the API dialog in jsreport studio for details</param>
        /// <returns>Report result promise</returns>
        public Report Render(object request)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("LocalReportingService not initialized. Call initialize first");
            }


            var settings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
                    PreserveReferencesHandling = PreserveReferencesHandling.All
                };

            var requestString = JsonConvert.SerializeObject(request, settings);

            var worker = new Process()
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(AbsolutePathToServer, "node.exe"))
                        {
                            FileName = Path.Combine(AbsolutePathToServer, "node.exe"),
                            WorkingDirectory = AbsolutePathToServer,
                            Arguments = "server.js",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        }
                };
            
            worker.StartInfo.EnvironmentVariables.Add("JSREPORT_REQUEST", requestString);
            worker.StartInfo.EnvironmentVariables.Remove("NODE_ENV");
            worker.StartInfo.EnvironmentVariables.Add("NODE_ENV", "production");

            var outputPath = "";
            var logs = "";

            worker.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null) return;

                    Console.WriteLine(e.Data);
                    if (e.Data.StartsWith("$output="))
                    {
                        outputPath = e.Data.Split(new[] {"$output="}, StringSplitOptions.None)[1];
                    }
                    else
                    {
                        logs += e.Data;
                    }
                };

            worker.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        logs += e.Data;
                    }
                };


            worker.Start();
            worker.BeginOutputReadLine();
            worker.BeginErrorReadLine();

            worker.WaitForExit();

            if (worker.ExitCode == 1)
            {
                worker.Close();
                throw new InvalidOperationException("Rendering report failed: " + logs);
            }
            worker.Close();

            return new Report() {Content = new FileStream(outputPath, FileMode.Open)};
        }

        private void Decompress()
        {
            var zippedFromSolution =
                new FileInfo(Path.Combine(AssemblyDirectory, "jsreport", "production", "jsreport.zip"));
            var zippedFromApp = new FileInfo(Path.Combine(AbsolutePathToServer, "jsreport.zip"));
            var isSame = zippedFromApp.Exists && zippedFromSolution.Exists &&
                             (zippedFromApp.Length == zippedFromSolution.Length);


            if (!Directory.Exists(Path.Combine(AbsolutePathToServer, "node_modules")) || !isSame)
            {
                DeleteDirectoryContent(AbsolutePathToServer);

                var fileToDecompress =
                    new FileInfo(Path.Combine(AssemblyDirectory, "jsreport", "production", "jsreport.zip"));

                if (!fileToDecompress.Exists)
                    throw new InvalidOperationException(fileToDecompress.FullName + " file not found.");

                new DirectoryInfo(AbsolutePathToServer).Delete(true);
                ZipFile.ExtractToDirectory(fileToDecompress.FullName, AbsolutePathToServer);
            }

            CopyFilesRecursively(new DirectoryInfo(Path.Combine(AssemblyDirectory, "jsreport", "reports")),
                                 new DirectoryInfo(Path.Combine(AbsolutePathToServer, "../reports")));

            File.Copy(Path.Combine(AssemblyDirectory, "jsreport", "production", "server.js"),
                      Path.Combine(AbsolutePathToServer, "server.js"), true);
            File.Copy(Path.Combine(AssemblyDirectory, "jsreport", "production", "prod.config.json"),
                      Path.Combine(AbsolutePathToServer, "prod.config.json"), true);
            File.Copy(Path.Combine(AssemblyDirectory, "jsreport", "production", "jsreport.zip"),
                      Path.Combine(AbsolutePathToServer, "jsreport.zip"), true);
        }

        private static bool IsWebApp(AppDomain appDomain)
        {
            var configFile = (string) appDomain.GetData("APP_CONFIG_FILE");
            if (string.IsNullOrEmpty(configFile)) return false;
            return (
                       Path.GetFileNameWithoutExtension(configFile) ?? string.Empty
                   ).Equals(
                       "WEB",
                       StringComparison.OrdinalIgnoreCase);
        }

        private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        private void DeleteDirectoryContent(string path, bool skipMainDir = true)
        {
            if (!Directory.Exists(path))
                return;

            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectoryContent(directory, false);
            }

            try
            {
                Directory.EnumerateFiles(path).ToList().ForEach(File.Delete);
            }
            catch (IOException)
            {
                Directory.EnumerateFiles(path).ToList().ForEach(File.Delete);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.EnumerateFiles(path).ToList().ForEach(File.Delete);
            }

            if (skipMainDir)
            {
                return;
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
    }
}