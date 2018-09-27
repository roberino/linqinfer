using System.Collections.Generic;
using LinqInfer.Utility.Expressions;
using System.IO;
using System.Linq;

namespace LinqInfer.Compiler
{
    class SourceCodeRepository : ISourceCodeProvider
    {
        static readonly Dictionary<string, string> SupportedExtensions = new Dictionary<string, string>()
        {
            ["fky"] = KnownMimeTypes.Function,
            ["json"] = KnownMimeTypes.Json
        };

        readonly DirectoryInfo _sourceDirectory;

        public SourceCodeRepository(DirectoryInfo sourceDirectory)
        {
            _sourceDirectory = sourceDirectory;
        }

        public bool Exists(string name)
        {
            var file = GetFile(name);

            return file.Exists;
        }

        public SourceCode GetSourceCode(string name)
        {
            var file = GetFile(name);

            if (!file.Exists)
            {
                return SourceCode.NotFound(name);
            }

            using (var reader = file.OpenText())
            {
                return SourceCode.Create(name, reader.ReadToEnd(), SupportedExtensions[file.Extension.Substring(1)]);
            }
        }

        FileInfo GetFile(string name)
        {
            FileInfo firstFile = null;

            foreach (var type in SupportedExtensions.OrderBy(kv => kv.Value == KnownMimeTypes.Function ? 0 : 1))
            {
                var file = GetFile(name, type.Key);

                if (firstFile == null)
                {
                    firstFile = file;
                }

                if (file.Exists)
                {
                    return file;
                }
            }

            return firstFile;
        }

        FileInfo GetFile(string name, string ext)
        {
            return new FileInfo(Path.Combine(_sourceDirectory.FullName,
                name.Replace('.', Path.DirectorySeparatorChar) + "." + ext));
        }
    }
}