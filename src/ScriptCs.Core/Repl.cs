using Common.Logging;
using ScriptCs.Contracts;
using ServiceStack.Text;
using System;
using System.IO;
using System.Runtime.ExceptionServices;

namespace ScriptCs
{
    public class Repl : ScriptExecutor
    {
        private readonly string[] _scriptArgs;

        public Repl(
            string[] scriptArgs,
            IFileSystem fileSystem,
            IScriptEngine scriptEngine,
            ILog logger,
            IConsole console,
            IFilePreProcessor filePreProcessor,
            IReplCommandService replCommandService) : 
            base(fileSystem, filePreProcessor, scriptEngine, logger, replCommandService)
        {
            _scriptArgs = scriptArgs;
            Console = console;
        }

        public string Buffer { get; set; }

        public IConsole Console { get; private set; }

        public override void Terminate()
        {
            base.Terminate();
            Logger.Debug("Exiting console");
            Console.Exit();
        }

        public override ScriptResult Execute(string script, params string[] scriptArgs)
        {
            try
            {
                ScriptResult result;

                if (script.StartsWith("#") || script.StartsWith(":"))
                {
                    if (!script.StartsWith("#r ") && !script.StartsWith("#load "))
                    {
                        if (ProcessCoreCommand(script, out result)) return result;

                        result = ReplCommandService.ProcessCommand(script);

                        if (result.ExecuteExceptionInfo != null)
                        {
                            result.ExecuteExceptionInfo.Throw();
                        }
                        else if (result.CompileExceptionInfo != null)
                        {
                            result.CompileExceptionInfo.Throw();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(result.ReturnValue.ToJsv());
                            Buffer = null;
                            return result;
                        }
                    }
                }
                
                var preProcessResult = FilePreProcessor.ProcessScript(script);

                ImportNamespaces(preProcessResult.Namespaces.ToArray());

                foreach (var reference in preProcessResult.References)
                {
                    var referencePath = FileSystem.GetFullPath(Path.Combine(Constants.BinFolder, reference));
                    AddReferences(FileSystem.FileExists(referencePath) ? referencePath : reference);
                }

                Console.ForegroundColor = ConsoleColor.Cyan;

                Buffer += preProcessResult.Code;

                result = ScriptEngine.Execute(Buffer, _scriptArgs, References, DefaultNamespaces, ScriptPackSession);
                if (result == null) return new ScriptResult();

                if (result.CompileExceptionInfo != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(result.CompileExceptionInfo.SourceException.Message);
                }

                if (result.ExecuteExceptionInfo != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(result.ExecuteExceptionInfo.SourceException.Message);
                }

                if (result.IsPendingClosingChar)
                {
                    return result;
                }

                if (result.ReturnValue != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(result.ReturnValue.ToJsv());
                }

                Buffer = null;
                return result;
            }
            catch (FileNotFoundException fileEx)
            {
                RemoveReferences(fileEx.FileName);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\n" + fileEx + "\r\n");
                return new ScriptResult { CompileExceptionInfo = ExceptionDispatchInfo.Capture(fileEx) };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\n" + ex + "\r\n");
                return new ScriptResult { ExecuteExceptionInfo = ExceptionDispatchInfo.Capture(ex) };
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private bool ProcessCoreCommand(string script, out ScriptResult scriptResult)
        {
            scriptResult = new ScriptResult();
            var executedCoreCommand = false;
            try
            {
                if (script.StartsWith("#reset") ||
                    script.StartsWith(":reset"))
                {
                    Reset();
                    executedCoreCommand = true;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\n" + ex + "\r\n");
                scriptResult = new ScriptResult {ExecuteExceptionInfo = ExceptionDispatchInfo.Capture(ex)};
            }
            return executedCoreCommand;
        }
    }
}
