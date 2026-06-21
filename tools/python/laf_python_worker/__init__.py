"""LAF Python worker subsystem.

A small, SAFE set of deterministic-where-possible workers invoked by the C# SafePythonWorkerRunner over
JSON stdin/stdout. There is no arbitrary-script execution: only `main.py` is an entrypoint and it dispatches
to the fixed, approved task set. No network access except through the allowlisted scraper. No writes outside
the approved working directory.
"""
__version__ = "1.0.0"
