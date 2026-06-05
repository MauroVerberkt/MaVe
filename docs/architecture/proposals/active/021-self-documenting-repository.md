---
sidebar_position: 21
title: "PROP-021: Self-Documenting Repository"
tags: [infra]
---

# PROP-021: Self-Documenting Repository

**Status:** exploring  
**Size:** large  
**Created:** 2026-06-05  

## Problem / Motivation

This repository already has structured documentation: XML docs, ADRs, proposals, design
documents, and agent instructions. But that structure is navigable by humans and partially
by AI agents through direct file access. It is not queryable by meaning, it does not
expose relationships between entities, and there is no standard way for a tool or agent
to understand what the repository contains without reading everything.

The goal is to establish a **Way of Working** where a repository is its own knowledge
base. No external wiki, no Confluence, no context that lives outside the repo. Everything
an agent, a developer, or a tool needs to understand, navigate, and search the repository
is derived from the repository itself and stored alongside it.

This is both a practical improvement to MaVe and a proof-of-concept for a replicable
pattern. MaVe has been an experiment in AI-native repository design. This proposal is
the culmination of that experiment: making the repo genuinely self-documenting in the age
of AI-assisted development.

## Vision

A repository that any agent can pick up and immediately understand:

- **"What is this repo?"** → answered by `llms.txt`, `.opencode/instructions.md`,
  and `.ai/manifest.json`
- **"What does it contain and how is it organised?"** → answered by the knowledge graph
- **"What's relevant to my task?"** → answered by vector embeddings (semantic search)
- **"Where is the exact text/code?"** → answered by the full-text search index (or grep)

These are four distinct queries with four distinct tools. They are complementary and do
not replace each other.

## Sketch

### Repository structure

```
.ai/                              # All derived knowledge artefacts
├── manifest.json                 # Repository map: what exists, where, how to navigate
├── graph/
│   ├── entities.json             # All named entities: packages, files, types, docs, ADRs
│   └── relationships.json        # Typed edges between entities
├── embeddings/
│   ├── model-info.json           # Model name, dimensions, normalisation, how to query
│   └── index.jsonl               # One record per chunk: { id, text, embedding, metadata }
├── search/
│   └── index.json                # Full-text inverted index (keyword search)
└── generate.ps1                  # Single script that rebuilds everything from source
```

Everything in `.ai/` is **derived**. The source of truth is always the actual content
(source code, docs, architecture files). The generation script is the contract between
content and index.

### Layer 1 — Knowledge Graph (highest priority)

The graph captures structural understanding: what entities exist and how they relate.

**Entity types:**

| Type | Examples |
|------|---------|
| `package` | `Monads`, `BusinessRules`, `Unions` |
| `namespace` | `MaVe.BusinessRules.Attributes` |
| `type` | `BusinessRule<T>`, `Result<T>`, `Option<T>` |
| `document` | `docs/monads/result.md` |
| `adr` | `ADR-001`, `ADR-012` |
| `proposal` | `PROP-018`, `PROP-021` |
| `decision` | architectural choices extracted from ADRs |

**Relationship types:**

| Relationship | Example |
|-------------|---------|
| `documents` | `docs/monads/result.md` → `Result<T>` |
| `implements` | `BusinessRule<T>` → `PROP-005` |
| `supersedes` | `ADR-014` → `ADR-007` |
| `depends-on` | `BusinessRules.ResultExtensions` → `Monads` |
| `extends` | `OrderAmountExceeded` → `BusinessRule<T>` |
| `part-of` | `BusinessRuleBase` → `BusinessRules` (package) |
| `references` | `PROP-020` → `BusinessRulesGenerator` |

**Initial approach:** Curate the graph manually (the relationships are known and stable).
Automate incrementally — Roslyn can extract type/namespace relationships, frontmatter
parsers can extract document relationships, `Directory.Build.props` can supply package
dependency edges.

**Format (entities.json):**

```json
[
  {
    "id": "pkg:Monads",
    "type": "package",
    "label": "Monads",
    "path": "src/Monads",
    "description": "Result<T> and Option<T> monadic types"
  }
]
```

**Format (relationships.json):**

```json
[
  {
    "from": "doc:docs/monads/result.md",
    "to": "type:Result<T>",
    "rel": "documents"
  },
  {
    "from": "pkg:BusinessRules.ResultExtensions",
    "to": "pkg:Monads",
    "rel": "depends-on"
  }
]
```

### Layer 2 — Vector Embeddings

Enables semantic search: find content by meaning rather than exact keywords.

**What gets embedded:**

- Documentation pages (chunked by section)
- Architecture documents: ADRs, proposals, design docs
- XML doc comments from public API surfaces
- README and instruction files

Source code bodies (method implementations) are lower priority — agents already have
grep and can read files directly.

**Tooling decision (open):**

| Option | Pros | Cons |
|--------|------|------|
| Local model (ONNX, sentence-transformers) | No API key, runs offline, reproducible builds | Larger setup, model download |
| API-based (OpenAI, Cohere) | Higher quality, easy to set up | Costs money, external dependency, rebuilds cost per token |
| Hybrid | Best of both | Complexity |

For a WoW that works on any repo without external dependencies, a local model is
preferred. The exact model is an open question.

**Index format (index.jsonl):**

```jsonl
{"id": "doc:monads/result#map-section", "text": "Map applies a function...", "embedding": [0.1, 0.2, ...], "source": "docs/docs/monads/result.md", "section": "Map", "type": "documentation"}
{"id": "type:Result<T>#xmldoc", "text": "Generic result type representing...", "embedding": [...], "source": "src/Monads/Result.cs", "type": "api"}
```

**Consumer interface:** An agent can load the JSONL, compute a query embedding with the
same model, and rank by cosine similarity. No vector database required for a repo of
this size.

### Layer 3 — Full-Text Search Index

Keyword search for when exact text matters more than semantic meaning.

- Inverted index: term → `[{ id, path, section, score }]`
- Generated from the same content as embeddings
- Useful as a fallback when semantic search is imprecise
- Lower priority than graph and embeddings given agents already have `rg`/`grep`

### The generation script

```powershell
# .ai/generate.ps1
# Rebuilds all derived knowledge artefacts from source.

param(
    [switch]$GraphOnly,
    [switch]$EmbeddingsOnly,
    [switch]$SearchOnly
)

# 1. Scan source and docs, build entity/relationship lists
# 2. Write graph/entities.json and graph/relationships.json
# 3. Chunk content, compute embeddings, write embeddings/index.jsonl
# 4. Build inverted index, write search/index.json
# 5. Write manifest.json summarising what was generated
```

A single entry point. The script is the contract. Running it should always produce a
consistent, up-to-date set of artefacts.

### manifest.json

A machine-readable description of the repository for agents that pick it up cold:

```json
{
  "name": "MaVe",
  "description": "Functional patterns for .NET: Result, Option, Unions, BusinessRules",
  "generated": "2026-06-05T10:00:00Z",
  "packages": ["Monads", "Unions", "BusinessRules", "BusinessRules.ResultExtensions", "BusinessRules.Wcf"],
  "entryPoints": {
    "docs": "docs/",
    "architecture": "docs/architecture/",
    "source": "src/",
    "tests": "tests/",
    "agentInstructions": ".opencode/instructions.md"
  },
  "index": {
    "graph": ".ai/graph/",
    "embeddings": ".ai/embeddings/index.jsonl",
    "search": ".ai/search/index.json"
  }
}
```

### Rebuild triggers

| Trigger | Scope | Priority |
|---------|-------|----------|
| Manual (`generate.ps1`) | Full rebuild | Now |
| CI on merge to main | Full rebuild, commit artefacts | Phase 2 |
| Pre-commit hook | Incremental (changed files only) | Phase 3 |

## Open Questions

- **Embedding model:** Which local model gives the best quality/size tradeoff for
  technical documentation and C# code? Candidates: `all-MiniLM-L6-v2`,
  `nomic-embed-text`, `mxbai-embed-large`.
- **Chunking strategy:** How to chunk documentation pages? By heading? By paragraph?
  Fixed token window with overlap? Different strategies for docs vs. code vs. ADRs?
- **Graph automation:** How much of the knowledge graph can be derived automatically
  vs. curated manually? Start manual, automate incrementally, or design the schema
  around what can be automated from day one?
- **Commit artefacts or generate on demand?** Committing `.ai/` makes the knowledge
  base instantly available without tooling but adds binary/large files to the repo.
  Generating on demand requires agents to have the generation toolchain available.
- **Consumer interface:** Raw file access is enough for now. Should there eventually
  be an MCP server that wraps the index for structured querying?
- **WoW portability:** What is the minimum required structure a repo needs before
  this pattern can be applied? What are the preconditions?

## Prior Art / References

- [llmstxt.org](https://llmstxt.org) — well-known path convention for AI-readable
  project descriptions
- [Model2Vec](https://github.com/MinishLab/model2vec) — fast local embedding models
  suitable for offline use
- [Cognee](https://github.com/topoteretes/cognee) — knowledge graph construction from
  unstructured documents
- [Continue.dev context providers](https://docs.continue.dev/customization/context-providers) — how
  coding agents consume repo-level context today
- [LlamaIndex](https://www.llamaindex.ai/) — document ingestion and retrieval
  pipelines, useful reference for chunking and indexing strategies
- [Simon Willison on llms.txt](https://simonwillison.net/2024/Sep/26/llmstxt/) — good
  framing of the discoverability problem

## Outcome

_Filled when status changes to done/parked. Link to ADR(s) if applicable._
