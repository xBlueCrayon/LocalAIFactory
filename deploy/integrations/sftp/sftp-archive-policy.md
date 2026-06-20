# SFTP Archive Policy

The archive is the system of record for what was exchanged. It exists so that processing is
**idempotent**, **auditable**, and **replayable**. This policy describes how files move into
the archive and what guarantees it provides.

## Archive-after-success (never before)

- A file is moved to `ArchiveDir` **only after** it has been fully and successfully
  processed (parsed, persisted, response emitted where applicable).
- A failure during processing leaves the source **in place** or moves it to a quarantine
  area for inspection — it is **never** archived as if it succeeded. This prevents silent
  data loss.
- Outbound: a file we send is archived after the partner has confirmed (or after upload +
  `.done` marker is written and the transfer is verified, per the agreed contract).

## `.done` markers

- A payload is only eligible for processing once its `<file><DoneMarkerSuffix>` marker exists
  (default suffix `.done`). This guards against reading a half-written file.
- When archiving, move (or record) both the payload and its marker together so the archived
  set is self-describing.

## Hashing & idempotency key

- Compute a **SHA-256** over each file's content on receipt and on archive.
- The idempotency key is `filename + SHA-256` (or an agreed business key plus hash).
- Before processing, check the key against already-processed records: if it matches a
  completed entry, **skip** (this is how replay and re-delivery stay safe).
- Store the hash with the archived file's metadata so integrity can be re-verified later.

## Naming convention

Archive with a name that is sortable and traceable. A practical scheme:

```
<archive>/<yyyy>/<MM>/<dd>/<originalName>__<receivedUtc:yyyyMMddTHHmmssZ>__<sha256-12>.dat
<archive>/<yyyy>/<MM>/<dd>/<originalName>__<receivedUtc>.done
```

- Date-partitioned folders keep directory sizes manageable and make retention pruning simple.
- The embedded timestamp + short hash makes each archived name unique and collision-free even
  if the original filename repeats.

## Retention

- Define a retention window per partner/feed that meets your regulatory and operational needs
  (e.g. keep N months hot, then move to cold storage, then purge on the legal schedule).
- Retention is enforced against the date-partitioned folders; never delete a file that is
  still within an open dispute/audit window.
- Purges should be logged (what, when, by whom/which job) for audit.

## Replay-from-archive

- Replay is a **deliberate operator action**, not an automatic behavior.
- To replay: copy the archived original back into `InboundDir` (or a dedicated replay dir).
  The idempotency check will normally skip an already-processed file, so a genuine
  reprocess requires clearing/forcing that key — making replay explicit and intentional.
- Always record who initiated a replay and why (audit trail).

## Summary guarantees

| Guarantee | Mechanism |
|-----------|-----------|
| No partial reads | `.done` marker before eligibility |
| No double-processing | `filename + SHA-256` idempotency key |
| No silent loss | archive only after success; quarantine on failure |
| Auditable | date-partitioned, hashed, timestamped names + logs |
| Replayable | archived originals + deliberate replay action |
