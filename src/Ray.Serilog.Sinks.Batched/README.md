# Batch queue compatibility repair

This project contains the shared batch core from [RayWangQvQ/Ray.Serilog.Sinks](https://github.com/RayWangQvQ/Ray.Serilog.Sinks), commit `a61514ea002ec2743c4861fff0dd1583883aa45b` (package 0.1.5), under its GNU GPL v3 license. The original license is retained here.

Modified in this project to preserve every queued event at finite batch boundaries, drain all accepted events during explicit flush and logger disposal, and coordinate enqueue/removal/disposal. Automatic flush errors are observed. A failed batch is reported without automatic retransmission.

The assembly name, assembly version, public and protected API, group keys, message formatting and channel implementations remain compatible with the existing 0.1.5 notification packages. Both hosts resolve this project through Infrastructure; all existing channel packages use the same repaired core. No notification protocol or credentials are copied into this project.

The temporary source replacement can be removed when the upstream dependency provides and verifies these repairs. `BatchQueueIntegrityTests` covers the shared queue and its shutdown behavior.
