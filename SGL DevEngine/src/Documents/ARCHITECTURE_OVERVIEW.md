# Architecture Overview - SGL DevEngine

## System Layer Stack

```
┌─────────────────────────────────────────────────────────────┐
│                   UI Layer (WPF)                             │
│  Blueprint Editor | Code Viewer | Execution Monitor | AI    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│               Integration Layer (UIIntegration)              │
│  Tab Controllers | Graph Renderer | State Synchronizer      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│            Business Logic Layer (Core Engines)               │
│                                                              │
│  Graph Engine     Type System      Code Generation           │
│  ─────────────    ───────────      ──────────────            │
│  • Nodes/Edges    • 13 Port Types  • IR Compiler            │
│  • Cycle Detect   • Validation     • C# Generator           │
│  • Topological    • Conversion     • Python Generator       │
│  • Connections    • Registry       • C++ Generator          │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│          Execution Layer (BlueprintRuntime)                  │
│                                                              │
│  Virtual Machine    Node Executor Registry  Context Manager  │
│  ──────────────     ──────────────────      ──────────────   │
│  • Bytecode VM      • HttpExecutor          • Variables      │
│  • Graph Compiler   • DataTransformExecutor • Port Values    │
│  • Async Runtime    • DatabaseExecutor      • Execution Log  │
│  • Event Loop       • EventPublisherExecutor• Error Handling │
│                     • ControlFlowExecutor   • Depth Tracking │
│                     • AIInferenceExecutor   • Performance    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│         Infrastructure Layer (External Systems)              │
│                                                              │
│  HTTP Client       Event Bus        LLM Provider             │
│  ────────────      ────────────     ────────────             │
│  • .NET HttpClient • Pub/Sub        • Claude API            │
│  • Retry Logic     • Async Events   • OpenAI API            │
│  • Timeouts        • Subscriptions  • Local Templates        │
│  • SSL/TLS         • Threading      • Caching               │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Graph Engine
**Purpose**: Blueprint data model and operations

**Responsibilities**:
- Node/Edge creation and management
- Graph validation (DAG, type checking)
- Cycle detection (DFS algorithm)
- Topological sorting
- Graph metadata & versioning

**Key Classes**:
- `Graph`: Container for nodes/edges/ports
- `GraphNode`: Individual node with properties
- `GraphEdge`: Connection between nodes
- `PortDefinition`: Input/Output port specification

**Scalability**:
- Supports 1000s of nodes efficiently
- O(V+E) complexity for standard operations
- Lazy-load subgraphs for large designs

### 2. Type System
**Purpose**: Port type validation and type safety

**Responsibilities**:
- 13 pre-defined port types
- Type assignability checking
- Implicit conversion rules
- Type registry system

**Port Types**:
- Integer, Float, String, Boolean, Object, JSON
- Event, Stream, AIEmbedding, Binary, DateTime
- Array, Dictionary

**Type Safety**:
- Prevents incompatible connections at design-time
- Runtime validation during execution
- Extensible for custom types

### 3. Code Generation
**Purpose**: Convert graphs to executable code

**Responsibilities**:
- IR (Intermediate Representation) compilation
- Multi-language code generation
- Template-based approach
- Error handling code generation

**Supported Languages**:
- C# (for .NET ecosystem)
- Python (for ML/data science)
- C++ (for high-performance)

**Generation Pipeline**:
1. Graph → IR (language-neutral AST)
2. IR → Language-specific AST  
3. AST → Source code (templates)
4. Code → Formatted output

### 4. Blueprint Runtime
**Purpose**: Execute compiled graphs

**Responsibilities**:
- Graph execution scheduling
- Node executor invocation
- Context management
- Performance tracking

**Execution Model**:
1. Topological sort of graph
2. Sequential node execution (parallelizable in future)
3. Port value propagation
4. Exception handling & recovery

**Node Executors**:
- HttpRequestExecutor: HTTP/REST calls
- DataTransformExecutor: String manipulation
- DatabaseQueryExecutor: SQL execution
- EventPublisherExecutor: Event bus integration
- ControlFlowExecutor: If/Loop/Switch logic
- AIInferenceExecutor: LLM integration

### 5. AI Builder
**Purpose**: Auto-generate architectures from natural language

**Responsibilities**:
- Prompt parsing & understanding
- Architecture generation
- Service node creation
- Template application

**LLM Integration**:
- Claude 3 Opus (cloud)
- Local template fallback
- Custom fine-tuning support

### 6. Event Bus
**Purpose**: Async pub/sub communication

**Responsibilities**:
- Event subscriptions
- Async message delivery
- Exception isolation
- Thread-safe handler management

**Event Types**:
- GraphLoadedEvent
- NodeExecutedEvent
- ErrorEvent
- Custom user events

---

## Data Flows

### Design Time Flow
```
User Input
    ↓
UI Layer (MainWindow)
    ↓
Graph Engine (Add Node/Edge)
    ↓
Type System (Validate Connection)
    ↓
Event Bus (GraphModifiedEvent)
    ↓
UI Layer (Render Update)
    ↓
User Sees Blueprint
```

### Code Generation Flow
```
User Clicks "Generate Code"
    ↓
UIIntegration.GenerateCode()
    ↓
Get Current Graph
    ↓
CSharpGenerator.Generate() / PythonGenerator / CppGenerator
    ↓
GraphCompiler.CompileToIR()
    ↓
Language-specific code generation
    ↓
Format & syntax highlighting
    ↓
Display in Code Viewer tab
```

### Execution Flow
```
User Clicks "Execute"
    ↓
UIIntegration.ExecuteGraph()
    ↓
BlueprintRuntime.Execute()
    ↓
Validate Graph (cycle check)
    ↓
Get Topological Order
    ↓
For Each Node:
    ├─ Get Node Executor
    ├─ Call Executor.Execute()
    ├─ Store output values
    └─ Continue or error
    ↓
Collect Results
    ↓
Display in Execution tab
```

### AI Generation Flow
```
User Enters Prompt
    ↓
AIGraphBuilder.BuildFromPrompt()
    ↓
LocalLLMProvider.GenerateArchitecture()
    ↓
Parse LLM Response
    ↓
Create Graph Nodes from Response
    ↓
Connect Nodes based on architecture
    ↓
Set Node Properties
    ↓
Return Generated Graph
    ↓
Display in AI tab
```

---

## Deployment Architecture

### Single-Machine Deployment
```
Windows Machine
├── SGLDevEngine.Studio.exe (148 KB)
├── Core DLLs (384 KB)
├── Dependencies (3.2 MB .NET Runtime)
└── Configuration
    └── Settings.json

Total: ~4 MB installed
```

### Multi-Machine Enterprise
```
Load Balancer
    ↓
Application Servers × N
    ├── SGLDevEngine API
    ├── Execution Nodes
    └── Cache Layer
    ↓
Shared Services
    ├── PostgreSQL (Graph Storage)
    ├── Redis (Session/Cache)
    ├── RabbitMQ (Event Bus)
    └── Elasticsearch (Audit Logs)
    ↓
External Services
    ├── Claude API (LLM)
    ├── Auth Provider (oauth)
    └── CDN (Assets)
```

---

## Database Schema

### Graphs Table
```sql
CREATE TABLE graphs (
    id NVARCHAR(36) PRIMARY KEY,
    name NVARCHAR(255),
    domain NVARCHAR(50),
    created_at DATETIME,
    modified_at DATETIME,
    owner_id NVARCHAR(36),
    metadata NVARCHAR(MAX)  -- JSON
);
```

### Nodes Table
```sql
CREATE TABLE nodes (
    id NVARCHAR(36) PRIMARY KEY,
    graph_id NVARCHAR(36),
    title NVARCHAR(255),
    type NVARCHAR(50),
    properties NVARCHAR(MAX),  -- JSON
    created_at DATETIME
);
```

### Edges Table
```sql
CREATE TABLE edges (
    id NVARCHAR(36) PRIMARY KEY,
    graph_id NVARCHAR(36),
    source_id NVARCHAR(36),
    target_id NVARCHAR(36),
    source_port NVARCHAR(100),
    target_port NVARCHAR(100)
);
```

### Execution History
```sql
CREATE TABLE executions (
    id NVARCHAR(36) PRIMARY KEY,
    graph_id NVARCHAR(36),
    started_at DATETIME,
    completed_at DATETIME,
    status NVARCHAR(20),  -- Success/Failed
    result NVARCHAR(MAX),  -- JSON
    error_message NVARCHAR(MAX)
);
```

---

## Security Architecture

### Authentication & Authorization
- JWT-based authentication
- Role-based access control (RBAC)
- Multi-factor authentication (MFA)
- Single sign-on (SAML/OAuth)

### Data Protection
- End-to-end encryption for graphs
- Secrets management integration
- Audit logging of all operations
- Compliance with SOC 2, ISO 27001

### Network Security
- HTTPS enforcement
- TLS 1.3 minimum
- API rate limiting
- DDoS protection

### Code Execution Sandboxing
- Node execution isolation
- Resource limits (CPU, memory)
- Network access control
- Timeout enforcement

---

## Performance Characteristics

### Graph Operations
| Operation | Time | Complexity |
|-----------|------|-----------|
| Add node | <1ms | O(1) |
| Add edge | <1ms | O(1) |
| Cycle detection | 10-50ms | O(V+E) |
| Topological sort | 5-30ms | O(V+E) |
| Validate types | 2-10ms | O(E) |

### Code Generation
| Language | Time | Output Size |
|----------|------|------------|
| C# | 150-300ms | 10-50KB |
| Python | 120-250ms | 12-60KB |
| C++ | 200-400ms | 15-80KB |

### Execution Wall Time
- HTTP request: 500ms-5s (network dependent)
- Data transform: <1ms
- Database query: 100-2000ms (DB dependent)
- AI inference: 1-10s (LLM latency)

### Memory Footprint
| Component | Usage |
|-----------|-------|
| Runtime | 100-200 MB |
| Graph (1000 nodes) | 50-100 MB |
| Generated code | 1-10 MB |
| Execution context | 5-20 MB |

---

## Technology Stack

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| UI | WPF | Enterprise-grade, native Windows |
| Runtime | .NET 8 | Performance, cross-platform ready |
| Graph DB | N/A (In-memory) | Simplicity, easy serialization |
| LLM | Claude 3 | Best code understanding |
| HTTP | HttpClient | Built-in, battle-tested |
| Events | Custom Pub/Sub | Full control, extensible |
| Deployment | Docker/K8s | Enterprise standard |

---

## Extension Points

### Custom Node Types
1. Implement `INodeExecutor` interface
2. Register with runtime
3. Define input/output ports
4. Handle properties

### Custom Port Types
1. Extend `PortTypeKind` enum
2. Add type registry entry
3. Define conversion rules
4. Update code generators

### Custom Code Generators
1. Inherit from `ILanguageGenerator`
2. Implement template files
3. Register with system
4. Support code generation pipeline

---

## Monitoring & Observability

### Metrics
- Graph execution count & duration
- Code generation success rate
- API error rates
- User engagement metrics

### Logging
- Structured JSON logging
- Execution trace logs
- Error/exception logs
- Audit logs (compliance)

### Health Checks
- Runtime status
- Database connectivity
- External service health
- Resource utilization

---

## Future Architecture Enhancements

### Phase 2 (2026 H2)
- Distributed execution (multi-machine graph parallelization)
- Graph caching & incremental compilation
- Performance profiler & optimization recommendations
- Advanced error recovery & retry policies

### Phase 3 (2027)
- Graph streaming (100k+ node support)
- Query language for graph traversal
- Advanced clustering & partitioning
- GPU acceleration for AI workloads

