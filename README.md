This project is a learning-focused implementation of the Saga pattern using a simple orchestration approach.

The goal is not to provide a production-ready solution, but to understand how long-running workflows, partial failures, and compensating transactions behave in practice.

What this project demonstrates

A simple Saga Orchestrator

Explicit Saga state tracking in a database

Step-by-step workflow progression

Compensation logic when a step fails

Use of an Outbox table to drive Saga progression

Both success and failure paths executed end-to-end

Business flow (simplified)

Order is created

Inventory is reserved

Payment is processed

Success path

Order moves to Completed

Saga finishes successfully

Failure path

Payment fails (simulated)

Inventory is released

Order is cancelled

Saga ends in a failed state after compensation

Key concepts explored

Why distributed workflows cannot rely on database rollbacks

How compensating actions differ from transactions

Why Saga steps must be independently committed

How state transitions make workflows observable

How Outbox-driven processing improves reliability

Why failure paths require intentional design

What I initially missed (and learned by building)

Success paths are straightforward; failure paths are not

Compensation needs to be explicit and idempotent

State visibility is critical for debugging and understanding flow

Retrying work without care can cause duplicate side effects

Reading about the Saga pattern is very different from implementing it

Technical stack

.NET Web API

Entity Framework Core

SQL Server (LocalDB)

BackgroundService for orchestration

Transactional Outbox Pattern

Notes

This repository is intentionally kept simple and self-contained. It exists as a learning artifact, not a production reference.
