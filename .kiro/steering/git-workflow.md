---
inclusion: always
---

# Git Workflow Rules

## Commit and Push After Changes

**CRITICAL RULE**: After completing any implementation work, code changes, or feature additions, you MUST:

1. **Check git status** to see what changed
2. **Stage all changes**: `git add .`
3. **Commit with descriptive message**: `git commit -m "description"`
4. **Push to remote**: `git push`

## When to Commit

Commit after:
- Completing a feature or task
- Fixing bugs
- Making configuration changes
- Updating documentation
- Any code modifications that should be saved

## Commit Message Format

Use clear, descriptive commit messages:
- Start with a verb (Add, Fix, Update, Remove, Refactor)
- Be specific about what changed
- Example: "Add dark mode support and fix Docker startup issues"

## Never Skip This Step

Even if the user doesn't explicitly ask for it, **ALWAYS** suggest committing and pushing after making changes. This ensures work is saved and shared with the team.

## Exception

Only skip if the user explicitly says "don't commit yet" or "I'll commit later".
