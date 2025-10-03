
# Contributing

Thank you for considering contributing to this project! We welcome contributions in the form of bug reports, feature requests, documentation improvements and pull requests.

## Before you begin

1. Ensure there is no existing issue addressing the same problem. If there is, feel free to add additional context or details.
2. Read through our [Code of Conduct](CODE_OF_CONDUCT.md) to understand our expectations for participant behavior.
3. For security related issues, see [SECURITY.md](SECURITY.md) and use the recommended disclosure channels instead of filing a public issue.

## Making Changes

1. Fork the repository and create your feature branch from `main`: `git checkout -b my-feature`.
2. If you've added code that should be tested, add tests.
3. Ensure the test suite passes by running `dotnet test`.
4. Format your code with `dotnet format` and ensure there are no linting warnings.
5. Update any relevant documentation (README, examples, etc.).

## Submitting a Pull Request

1. Open a pull request against the `main` branch. Describe what your change does and reference any related issues.
2. Fill out the pull request template checklist.
3. One of the project maintainers will review your changes and provide feedback.
4. Make any requested changes and update the pull request.
5. Once approved, your changes will be merged and included in the next release.

We appreciate your time and effort to improve this project! If you're unsure how to get started or have questions, feel free to open an issue to discuss your idea.




## Public API surface

This repository uses **Roslyn Public API Analyzers** to lock down the surface area.
When you add or change a public member in a packable project:

1. Make your code changes.
2. Run the bump script to update the `PublicAPI.Unshipped.txt` files:

   - Windows/PowerShell: `./build/bump-api.ps1`
   - macOS/Linux: `./build/bump-api.sh`

3. Review the diff and commit. When we cut a release, items from *Unshipped* will be moved to *Shipped*.

> Tips
> - We keep a single `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` pair per project across TFMs.
> - If a member is TFM-specific, append a trailing comment to its line: `// TFM: net8.0` or `// TFM: netstandard2.0`.

