# Sentinel architecture at Week 1

```text
HTTP client -> API -> Application contracts/services -> Infrastructure -> PostgreSQL
                                  ^
                                  |
                          Domain invariants

Worker host -> Application + Infrastructure (planned execution loop in Week 2)
```

The API and worker share the application and infrastructure assemblies but remain separate hosts. This keeps the worker independently deployable without introducing a distributed system before the product requires one.

