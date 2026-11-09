# ADR-0004: Content model and demo content as code

- Status: Accepted
- Date: 2026-11-10
- Issue: #1

## Problem

Umbraco keeps document types, templates and content in its database. If an editor creates them by hand,
no one else can reproduce the demo from the README, and changes to the content model cannot be reviewed in a pull request.

We need every developer, CI and later Azure to get the **same** content model with the **same keys**.
The Booking module will use the event's Umbraco `Key` as its stable event ID (ADR-0002), so keys must not change between environments.

## Options

1. **uSync**: the usual choice in Umbraco teams. It exports document types and content to files and imports them on startup.
2. **Code that calls Umbraco's editing services** at startup (`IContentTypeEditingService`, `IContentEditingService`, ...).
3. **Umbraco package migration** (`PackageMigrationPlan`) with an exported package.

## Decision

Option 2 for now: `DemoContentSeeder` runs on `UmbracoApplicationStartedNotification` and creates templates, document types and
content with fixed keys. Every step looks the item up by key first, so it is idempotent. It is enabled by
`GeekPass:DemoContent:Enabled` (on in Development only).

The Catan event is created as a **draft**. Publishing it is part of the editor demo. CI sets
`GeekPass:DemoContent:PublishCatanEvent=true` because nobody clicks Publish in CI.

## Trade-offs

- **Plus:** no third-party dependency, so no wait for a package release compatible with each Umbraco minor version.
  It uses the same services as the backoffice, so Umbraco validates the model. The C# is easy to review, and it teaches the Umbraco service APIs.
- **Plus:** fixed keys give stable IDs for the Booking integration.
- **Minus:** it only **creates**. A changed property on an existing document type needs an update step or a database reset.
  uSync handles updates and deletes for us.
- **Minus:** complex data types (Block Grid configuration) are verbose in code.
- **Minus:** changes an editor makes in the backoffice are not written back to the repository.

## Revisit when

- Issue #2 (Block Grid) makes the configuration in code hard to read, or
- we need to change existing document types often, or
- editors start to change the model in the backoffice.

Then we evaluate uSync, after checking that a release supports our Umbraco version.
