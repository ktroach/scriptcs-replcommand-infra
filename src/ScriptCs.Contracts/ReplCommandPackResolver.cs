using System.Collections.Generic;
using System.Linq;

namespace ScriptCs.Contracts
{
    public interface IReplCommandPackResolver
    {
        IDictionary<string, string> ReplCommands { get; }
        void AutoResolveReplCommandScriptPacks();
    }

    public sealed class ReplCommandPackResolver : IReplCommandPackResolver
    {
        private readonly IEnumerable<IScriptPackContext> _contexts;

        private const string ReplCommandPackContextName = "ReplCommandPackContext";
        private const string ReplCommandPackNamespace = "ScriptCs.ReplCommandPack";
        private const string ReplCommandsPropName = "ReplCommands";

        public IDictionary<string, string> ReplCommands { get; private set; }

        public ReplCommandPackResolver(IEnumerable<IScriptPackContext> scriptPackContexts)
        {
            _contexts = scriptPackContexts;

            AutoResolveReplCommandScriptPacks();
        }

        public void AutoResolveReplCommandScriptPacks()
        {
            foreach (var @dynamic in (from context in _contexts
                                      let contextType = context.GetType()
                                      where !string.IsNullOrWhiteSpace(contextType.Name)
                                      where contextType.Namespace != null &&
                                      (contextType.Name.Contains(ReplCommandPackContextName) ||
                                          contextType.Namespace.Equals(ReplCommandPackNamespace))
                                      select ((object)(context)))
                            .Select(ctx => new { ctx, replCommandsProperty = 
                                    ctx.GetType().GetProperty(ReplCommandsPropName) })
                            .Select(@t => @t.replCommandsProperty.GetValue(@t.ctx, null))
                            .Where(replCommandsDynamic => (dynamic)replCommandsDynamic != null))
            {
                ReplCommands = (dynamic)@dynamic;
            }
        }
    }
}
