# Project Instructions

This repository may be worked on by different AI agents,
models, sessions, accounts, and computers.

Git and committed repository files are the authoritative project state.
Do not rely on previous conversation memory.

## Branch context (2026-Q4)

- Active development branch: `sow/2026-Q4` (created 2026-10-06 from `master` `b4fce7c8`).
- `sow/2026-Q3` is frozen. It is the preserved record of the final Q3 release (SAM_Deploy `v20261006.1`,
  deploy SHA `1bee804f`). Do not commit to, rebase, rename, archive or delete it.
- `master` is the product line. It does not carry `AGENTS.md` or `PROJECT_PROGRESS.md`; these internal files live on
  `sow/*` branches only. Never merge or restore them onto `master`.
- Normal Q4 development continues from the normal SAM-BIM lineage. The unrelated HoareLea-compatible lineage
  (`contrib/sow-2026-Q3`) must never be merged or joined: no `--allow-unrelated-histories`, no forced history
  replacement, no rebasing normal SAM-BIM history onto HoareLea history.
- PR #8 (`feature/sam-gh-icon-redesign`, 104/104 icons) is known Q4 carry-over work. It is preserved and is not to be
  merged or retargeted without an explicit instruction.

## Before starting work

Before making significant changes:

1. Read this `AGENTS.md`.
2. Read `PROJECT_PROGRESS.md`.
3. Understand the relevant existing code, architecture, tests, and conventions.
4. Review the current Git branch and working tree.
5. Continue from the documented current state.

Prefer small, safe, targeted changes consistent with existing conventions.
Avoid unrelated refactoring unless it is required for the task.

## Project continuity

After each meaningful implementation, debugging, research,
testing, or validation checkpoint, update `PROJECT_PROGRESS.md`.

Do not update it for trivial actions such as opening files,
searching the repository, or reading documentation.

Keep `PROJECT_PROGRESS.md` concise and sufficient for another AI
agent on another computer to continue without access to the current conversation.

`PROJECT_PROGRESS.md` must contain:

- current status;
- work completed;
- important decisions and assumptions;
- files changed;
- tests, builds, checks, or validation performed and results;
- unresolved issues, risks, or blockers;
- exact recommended next step.

When updating `PROJECT_PROGRESS.md`, preserve still-relevant information
from previous sessions. Remove or replace information only when it is
obsolete, resolved, or superseded.

Before ending meaningful work, verify that `PROJECT_PROGRESS.md`
accurately represents the current repository state.

When appropriate, remind the user to commit and push changes before
switching computers, accounts, sessions, or AI agents.

Never assume conversation history, terminal history, generated files,
or uncommitted local changes will exist on another computer.
