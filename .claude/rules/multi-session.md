## Multi-Session Communication

**Critical**: When working across multiple Claude work units or sessions, you MUST use `.copilot_session_summary.md` to communicate progress and coordinate work.

### Session Summary Protocol
- **Always check**: Read `.copilot_session_summary.md` at the start of any work unit to understand previous progress
- **Always update**: Write to `.copilot_session_summary.md` at the end of each work unit with:
  - **What was completed**: Specific tasks, files modified, tests run, issues resolved
  - **What needs to be done**: Remaining tasks, known issues, next steps
  - **Current state**: Build status, test results, any blocking issues
  - **Context for next session**: Important findings, decisions made, approach taken

### Communication Format
```markdown
# Copilot Session Summary

## Last Updated
[Timestamp and session identifier]

## Completed Tasks
- [List of completed work items]
- [Files modified with brief description]
- [Tests run and results]

## Current Status
- Build status: [Success/Failed/Not tested]
- Test status: [Pass/Fail counts and any critical failures]
- Known issues: [Any problems discovered]

## Next Steps
- [Prioritized list of remaining tasks]
- [Any specific approaches or constraints to consider]
- [Dependencies or prerequisites for next work]

## Context Notes
- [Important decisions made]
- [Approaches that didn't work]
- [Key insights for future sessions]
```

### Best Practices
- **Update frequently**: Write to session summary after each significant milestone
- **Be specific**: Include file paths, command results, error messages
- **Think ahead**: Consider what the next Claude session will need to know
- **Preserve context**: Don't assume the next session has access to previous conversation history
