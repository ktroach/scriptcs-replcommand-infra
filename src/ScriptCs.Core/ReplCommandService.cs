using Common.Logging;
using ScriptCs.Contracts;
using System;
using System.Collections.Generic;

namespace ScriptCs
{
    public interface IReplCommandService
    {
        ScriptResult ProcessCommand(string command);
    }

    public class ReplCommandService : IReplCommandService
    {
        private readonly ILog _logger;
        private readonly IScriptEngine _scriptEngine;
        private readonly IDictionary<string, string> _replCommands = new Dictionary<string, string>();

        public ReplCommandService(IScriptEngine scriptEngine, ILog logger)
        {
            _scriptEngine = scriptEngine;
            _logger = logger;
        }

        public virtual ScriptResult ProcessCommand(string command)
        {
            _logger.Debug("Processing REPL Command [" + command + "] >>");

            var scriptResult = new ScriptResult();

            var scriptPacks = new List<IScriptPack>();
            var scriptPackSession = new ScriptPackSession(scriptPacks, null);

            if (_replCommands.Count.Equals(0))
            {
                if (scriptPackSession.ReplCommands != null 
                    && scriptPackSession.ReplCommands.Count > 0)
                {
                    foreach (var scriptPackReplCommand in scriptPackSession.ReplCommands)
                    {
                        _replCommands.Add(scriptPackReplCommand.Key, scriptPackReplCommand.Value);
                    }
                }
            }

            var script = ParseArguments(command);
            
            if (!string.IsNullOrWhiteSpace(script))
            {
                if (_scriptEngine != null)
                    scriptResult = _scriptEngine.Execute(script, 
                        new string[0], 
                        scriptPackSession.References, 
                        scriptPackSession.Namespaces, 
                        scriptPackSession);

                _logger.Debug("<< REPL Command executed.");
            }
            else
            {
                scriptResult.ReturnValue = "REPL Command not found";
                _logger.Debug("<< REPL Command not defined.");
            }

            return scriptResult;
        }

        protected virtual string ParseArguments(string command)
        {
            string script;
            var arguments = command.Split(new string[0], StringSplitOptions.None);
            var commandKey = arguments.Length > 0 ? arguments[0] : command;

            Guard.AgainstNullArgument("_replCommands", _replCommands);

            if (!_replCommands.ContainsKey(commandKey))
            {
                return string.Empty;
            }

            if (arguments.Length <= 0)
            {
                script = _replCommands[commandKey];
            }
            else
            {
                script = _replCommands[commandKey];
                var argumentCount = 0;
                foreach (var argument in arguments)
                {
                    if (argumentCount != 0)
                    {
                        var argumentToken = string.Format("arg{0}", argumentCount);
                        if (script.Contains(argumentToken))
                        {
                            script = script.Replace(argumentToken, argument);
                        }
                    }
                    argumentCount++;
                }
            }

            return script;
        }
    }
}
