using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.IO;

namespace LinqInfer.TestHarness
{
    public static class SigmajsServer
    {
        public static void Run()
        {
            var url = "http://localhost:8080";

            var physicalFileSystem = new PhysicalFileSystem((new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent).FullName);

            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = physicalFileSystem
            };

            options.StaticFileOptions.FileSystem = physicalFileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;

            WebApp.Start(url, builder => builder.UseFileServer(options));
            Console.WriteLine("Listening at " + url);
            Console.ReadLine();
        }
    }
}