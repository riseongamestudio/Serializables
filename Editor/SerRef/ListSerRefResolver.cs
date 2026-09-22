using Sirenix.OdinInspector.Editor;

namespace RiseOn.Serializables.Editor {
    [ResolverPriority(100)]
    public class ListSerRefResolver<TValue> : ProcessedMemberPropertyResolver<ListSerRef<TValue>> where TValue : class { }
}