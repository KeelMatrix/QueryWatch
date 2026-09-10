# Commit checks

Run `git config core.hooksPath .githooks` once per clone to enable the repository's local commit checks.

The versioned checks reject identity trailers and internal metadata in new commit messages. The public repository workflow checks the commits introduced by each push or pull request.
