# Messaging Architecture

## Message Queue
- Technology: System.Threading.Channels
- Pattern: Pub/Sub with topics
- Persistence: Optional SQLite backing

## Message Types
```csharp
public enum MessageType
{
    Command,
    Query,
    Event,
    Response,
    Stream
}
```

## Routing Rules
```yaml
routing:
  /plan: PlanningAgent
  /code: CodingAgent
  /review: ReviewAgent
  /test: TestingAgent
  /docs: DocumentationAgent
```

## Error Handling
- Retry with exponential backoff
- Dead letter queue
- Circuit breaker pattern
- Fallback responses

## Performance
- Target: < 100ms latency
- Throughput: 1000 msg/sec
- Concurrent connections: 100