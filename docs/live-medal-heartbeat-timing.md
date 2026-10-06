# Live medal heartbeat scheduling

Each accepted response supplies the timestamp and interval for the next heartbeat. The runner waits until their sum instead of sleeping a full interval after API responses and task-progress reads. This prevents request latency from accumulating across the session.

An overdue heartbeat restarts the entry session before sending. A rejected heartbeat also renews the session, resetting the sequence while retaining the device identity and the accepted watch-time budget. Retries stop at the configured consecutive-failure threshold. Explicitly rejected requests return their reserved daily watch allowance. A transport error with an unknown server outcome retains the reservation.

Heartbeat acceptance and platform task completion remain separate. The panel displays the latest platform task progress, which can advance in task-sized rounds instead of every minute. Offline rooms remain eligible for watching, but an accepted heartbeat alone does not mark their task complete.

The Web host writes local watch diagnostics under `config/live-watch-diagnostics`. Random session identifiers and keyed anonymous account/room tags associate entry, heartbeat, progress, observed live-state changes and stop events. Records contain concurrency, latest observed live state, accepted duration, confirmed progress, request timing and numeric response codes. Cookies, device values, signing keys, names and raw response messages are excluded. Files rotate at UTC+8 midnight, retain seven days and are capped at 4 MiB per day. Storage failures do not interrupt activities and these records bypass all notification sinks.

Watching uses one room at a time per account. Monitoring, scheduled execution and manual recovery share the same watch slot. The next room waits until the current session ends, then reloads platform task progress before starting. Accounts remain independent, and likes and danmaku do not occupy the watch slot. The former `LiveWatchDiagnostics:ConcurrentRooms` override is no longer used. Local diagnostics continue to record ordinary execution without starting comparison runs or extra platform calls.

The server-timestamp scheduling also appears in the [BLTH watching implementation](https://github.com/andywang425/BLTH/blob/master/src/modules/dailyTasks/liveTasks/medalTasks/watchTask.ts). Synthetic tests simulate API latency, slow task reads, delayed execution, rejected requests, persistent daily budgets, cancellation and confirmed platform completion. They make no activity or notification requests.
