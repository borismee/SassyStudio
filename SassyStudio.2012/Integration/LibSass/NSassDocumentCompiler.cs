﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NSass;
using SassyStudio.Compiler;
using SassyStudio.Options;

namespace SassyStudio.Integration.LibSass
{
    class NSassDocumentCompiler : IDocumentCompiler
    {
        static int IsResolverInitialized = 0;
        static readonly Encoding UTF8_ENCODING = new UTF8Encoding(true);
        readonly Lazy<ISassCompiler> _Compiler = new Lazy<ISassCompiler>(() => new SassCompiler());
        readonly ScssOptions Options;

        static NSassDocumentCompiler()
        {
            FixNSassAssemblyResolution();
        }

        public NSassDocumentCompiler(ScssOptions options)
        {
            Options = options;
        }

        private ISassCompiler Compiler { get { return _Compiler.Value; } }

        public FileInfo GetOutput(FileInfo source)
        {
            var filename = Path.GetFileNameWithoutExtension(source.Name);
            var directory = DetermineSaveDirectory(source);
            var target = new FileInfo(Path.Combine(directory.FullName, filename + ".css"));

            return target;
        }

        public void Compile(FileInfo source, FileInfo output)
        {
            IEnumerable<string> includePaths = new[] { source.Directory.FullName };
            if (!string.IsNullOrWhiteSpace(Options.CompilationIncludePaths) && Directory.Exists(Options.CompilationIncludePaths))
                includePaths = includePaths.Concat(Options.CompilationIncludePaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            var css = Compiler.CompileFile(source.FullName, sourceComments: Options.IncludeSourceComments, additionalIncludePaths: includePaths);
            File.WriteAllText(output.FullName, css, UTF8_ENCODING);
        }

        private DirectoryInfo DetermineSaveDirectory(FileInfo source)
        {
            if (string.IsNullOrWhiteSpace(Options.CssGenerationOutputDirectory))
                return source.Directory;

            var path = new Stack<string>();
            var current = source.Directory;
            while (current != null && ContainsSassFiles(current.Parent))
            {
                path.Push(current.Name);
                current = current.Parent;
            }

            // eh, things aren't working out so well, just go back to default
            if (current == null || current.Parent == null)
                return source.Directory;

            // move to sibling directory
            current = new DirectoryInfo(Path.Combine(current.Parent.FullName, Options.CssGenerationOutputDirectory));
            while (path.Count > 0)
                current = new DirectoryInfo(Path.Combine(current.FullName, path.Pop()));

            EnsureDirectory(current);
            return current;
        }

        private void EnsureDirectory(DirectoryInfo current)
        {
            if (current != null && !current.Exists)
            {
                EnsureDirectory(current.Parent);
                current.Create();
            }
        }

        private bool ContainsSassFiles(DirectoryInfo directory)
        {
            return directory != null && directory.EnumerateFiles("*.scss").Any();
        }

        private static void FixNSassAssemblyResolution()
        {
            if (Interlocked.CompareExchange(ref IsResolverInitialized, 1, 0) == 0)
            {
                var basePath = new FileInfo(new Uri(typeof(NSassDocumentCompiler).Assembly.CodeBase).LocalPath).Directory.FullName;
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    if (e.Name.StartsWith("NSass.Wrapper.proxy", StringComparison.Ordinal))
                        return Assembly.LoadFrom(Path.Combine(basePath, "NSass.Wrapper.x86.dll"));

                    return null;
                };
            }
        }
    }
}
