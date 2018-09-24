using LinqInfer.Utility.Expressions;
using System;
using System.IO;

namespace LinqInfer.Compiler
{
    class SourceCodeRepository : ISourceCodeProvider
    {
        readonly DirectoryInfo sourceDirectory;

        public SourceCodeRepository(DirectoryInfo sourceDirectory)
        {
            this.sourceDirectory = sourceDirectory;
        }

        public bool Exists(string name)
        {
            var file = GetFile(name);

            return file.Exists;
        }

        public string GetSourceCode(string name)
        {
            var file = GetFile(name);

            using (var reader = file.OpenText())
            {
                return reader.ReadToEnd();
            }
        }

        FileInfo GetFile(string name, string ext = ".fky")
        {
            return new FileInfo(Path.Combine(sourceDirectory.FullName, 
                name.Replace('.', Path.DirectorySeparatorChar) + ext));
        }
    }
}