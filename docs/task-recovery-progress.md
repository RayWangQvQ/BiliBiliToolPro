# Recovery progress

The Today page observes its current manual recovery through an async execution scope. Progress includes account checks, queued tasks, individual medal actions, sent interaction counts, accepted heartbeat time, and platform-confirmed task progress. Batch counts represent processed items rather than completed platform tasks.

Each account/task/action has a stable progress key. Parallel medal actions update their own rows, while unrelated background activities and other recovery requests remain isolated. A disposed observer cannot change a later recovery display. Viewer errors do not interrupt tasks.

Waiting conditions, exhausted configured budgets, room-state requirements, and active background actions are shown explicitly. Platform rejections retain their error codes and retry counts. Results remain visible after the final status refresh. Accepted heartbeat time is displayed separately from platform task completion.

Video watch and share API rejections produce failed recovery outcomes. Successful daily actions are followed by a platform status query before reporting task completion. Business reasons are shown without credential assignments or raw transport exception messages.

Manual and automatic recovery continue to suppress notifications. Recovery is serialized by account and task. Batch recovery runs up to four independent task groups concurrently, with short tasks scheduled before medal tasks. Each queued item rechecks saved switches, fresh completion status and automatic attempt limits before execution. Skipped items do not create execution records or consume retry attempts. Cancellation propagates without creating a failed execution record.

Explicitly rejected medal interactions release both the anchor allowance and the account like allowance. Both allowances are reserved together, and refund dates cannot affect a later day's budget. Requests reserve sending capacity without increasing confirmed completion. Only fresh platform progress debits the configured completion quota. Accepted or unresolved requests appear as pending confirmation, survive restart, and hold capacity for five minutes. A new run then releases aged pending reservations and reads fresh platform progress before retrying. Active sessions retain reservations until they finish. Raw send protection counters remain separate and survive reservation release and restart. Legacy request counts migrate as pending requests, never confirmed completion.

Each executed item receives its own dependency injection scope so mutable domain caches are not shared between concurrent account recoveries. The execution scope is disposed when that item finishes.

Incomplete recovery rows stay before completed rows, and failures remain first. Rows keep stable identities as live updates move completed actions down. Today task groups also list incomplete items before completed items after status refresh. Repeat recovery reads current platform progress and does not send completed medal actions again, including sessions that complete while waiting for a watch slot.

Budget messages identify panel-configured daily limits with their numeric values, separately from the tool's account like protection allowance and the platform's confirmed daily intimacy cap.

Progress-backed recovery records are written after the confirmation query. Unconfirmed outcomes are Pending, rejected actions are Failed, and only confirmed outcomes are Success. Starting any manual recovery focuses and scrolls to the recovery panel once. Subsequent progress updates preserve the viewer position.
