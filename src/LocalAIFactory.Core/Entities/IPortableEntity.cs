namespace LocalAIFactory.Core.Entities;

// KE-007 (Gap-2): the shared contract every portable knowledge node honors — a globally-unique,
// immutable Guid v7 identity used for cross-instance reconciliation and Knowledge Packs (E9). The lean
// M2 structural entities (CodeSymbol, DatabaseObject, DataDictionaryEntry, ApiEndpoint, ...) implement
// this too, alongside SourceArtifactId + SourceLocusKey — without the KnowledgeItem provenance/version
// chains (Gap-1 path a).
public interface IPortableEntity
{
    Guid Uid { get; set; }
}
