<!--
  Read AGENTS.md (agents) or CONTRIBUTING.md (humans) before filling this in.
  Keep it short. Merging mails this to everyone watching the repository.
-->

## What

Closes #

<!-- One or two sentences, then a line per file that moved. Not an essay. -->

## Scope

<!--
  L1 / L2 / L3. Every file this PR creates or modifies, one repo-relative path
  per line, inside the fence. CI compares this against the diff and FAILS on
  anything undeclared. Widening it is fine: edit the block, say why in a comment.
-->

```scope
```

## Verification (L7)

- [ ] `make check` - [ ] `make tests` - [ ] `make` - [ ] played it

<!--
  Real output, or name the run that covers it. Say which map you played and what
  happened. Name the tests you added, or why none apply.
-->

## Engine facts (L6)

<!--
  `file:line` for every engine claim, against the commit in ENGINE_BASE. Cite
  docs/ENGINE-NOTES.md where an entry exists; add entries for what you verified.
  One `UNVERIFIED:` line for anything you could not confirm. "None." if neither.
-->

## Determinism (L8)

- [ ] Does not touch simulation state
- [ ] Touches simulation state, and here is why it cannot desync:

## Checklist

- [ ] One task (L1), nothing unrelated (L2), smallest change that meets the DoD (L3)
- [ ] No new upstream file, or `engine-touch` label with justification (L4)
- [ ] Tunable behaviour in YAML, not hardcoded in C# (L5)
- [ ] Revertable with `git revert` alone (L10)
- [ ] Signed off (H3), AI-assisted work disclosed and labelled `ai-assisted` (A7)
