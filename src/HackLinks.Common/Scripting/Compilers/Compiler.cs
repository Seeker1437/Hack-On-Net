using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace HackLinks.Common.Scripting.Compilers
{
    public abstract class Compiler
    {
        /// <summary>
        /// List of pre-compilers that the scripts go through.
        /// </summary>
        public List<IPreCompiler> PreCompilers { get; protected set; }

        /// <summary>
        /// Compiles script or loads it from outPath, if cache is true and
        /// the script isn't newer.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="outPath"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public abstract Assembly Compile(string path, string outPath, bool cache);

        /// <summary>
        /// Creates new compiler
        /// </summary>
        protected Compiler()
        {
            PreCompilers = new List<IPreCompiler>();
        }

        /// <summary>
        /// Returns true if the out file exists and is newer than the script.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="outPath"></param>
        /// <returns></returns>
        protected bool ExistsAndUpToDate(string path, string outPath)
        {
            // Check existence of compiled assembly
            if (!File.Exists(outPath))
                return false;

            // Check if changes were made to script
            return File.GetLastWriteTime(path) <= File.GetLastWriteTime(outPath);
        }

        /// <summary>
        /// Saves assembly to outPath, overwrites existing file.
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="outPath"></param>
        protected void SaveAssembly(Assembly asm, string outPath)
        {
            var outRoot = Path.GetDirectoryName(outPath);

            if (File.Exists(outPath))
                File.Delete(outPath);
            else if (!Directory.Exists(outRoot))
                Directory.CreateDirectory(outRoot);

            File.Copy(asm.Location, outPath);
        }

        /// <summary>
        /// Runs script through all pre-compilers.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        protected string PreCompile(string script)
        {
            if (PreCompilers.Count == 0)
                return script;

            foreach (var precompiler in PreCompilers)
                script = precompiler.PreCompile(script);

            return script;
        }
    }
}
