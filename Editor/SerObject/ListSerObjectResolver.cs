using Sirenix.OdinInspector.Editor;

namespace RiseOn.Serializables.Editor {
    [ResolverPriority(100)]
    public class ListSerObjectResolver<TValue> : ProcessedMemberPropertyResolver<ListSerObject<TValue>> where TValue : class { }
}