# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [0.1.0] - 2026-08-12

### Added

- First public NuGet.org release of `KeelMatrix.QueryWatch` and `KeelMatrix.QueryWatch.EfCore` for N+1 detection, SQL/database performance budgets, EF Core interception, ADO.NET/Dapper instrumentation, assertions, and CI JSON summaries.
- Public `qwatch` .NET tool (`qwatch`) for query-count and SQL-duration gates, baselines, pattern budgets, and CI reporting.
- QueryWatch consumes the extracted `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` 0.1.0 packages from NuGet.org; the JSON contracts remain internal and are bundled into `qwatch` rather than published separately.
