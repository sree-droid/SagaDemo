**Saga + Outbox + Observability **

This project is a learning-focused implementation of:

* Saga pattern 

* Transactional Outbox

* Observability basics (correlation IDs + structured logs + debugging endpoints)

The goal is not a production-ready architecture, but to practice how workflows behave in success + failure paths — and how to debug them confidently when things go wrong.

**Why Observability was the next step**

After getting the Saga workflow working (including the failure + compensation path), I realized something important:

_Building the workflow is one thing.
Understanding what happened when it fails is another._

So I added observability features to answer questions like:

* “Where did the workflow fail?”

* “Did it retry?”

* “Did compensation run?”

* “Which logs belong to the same request/saga?”

**Saga style: Orchestration **

This project uses an orchestrator-based Saga, meaning:

* A central component tracks the workflow state

* It decides the next step

* It triggers compensating actions when something fails

In contrast, choreography-based Sagas have no central coordinator — services react to events independently. Orchestration was easier to reason about while learning and debugging.

**What the workflow does**

* OrderCreated

* InventoryReserved

* PaymentProcessed

* OrderCompleted

**Failure path**

If payment fails:

* release inventory

* cancel order

* saga ends in a failed state after compensation

**Outbox Pattern**

Instead of publishing events directly after database updates, this project writes:

* business data (Orders/SagaInstances)

* outbox messages (OutboxMessages)

…in the same transaction.

A background worker processes pending outbox rows and advances the saga.

**Observability features**
1) Correlation ID propagation

* API accepts or generates a X-Correlation-Id

* Correlation ID is returned in the response header

* Correlation ID is stored on outbox messages so async processing stays traceable

2) Structured logging with scopes

Logs are emitted with consistent context such as:

* CorrelationId

* SagaId

* OrderId

* Event type / step

This makes it possible to follow a single request across:
API → Outbox → Worker → Saga → Compensation
