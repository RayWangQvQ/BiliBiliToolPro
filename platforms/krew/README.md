# BiliBiliPro kubectl plugin

The `kubectl-bilipro` plugin deploys a Bilibili tool Deployment using bundled Kustomize resources. **Legacy integration:** its default image is `zai7lou/bilibili_tool_pro:2.0.1` and its `--login` path runs the old console DLL. This is not a deployment guide for the current Web image; verify image availability and compatibility before using it.

## Prerequisites

- A working Kubernetes cluster and a locally installed, configured `kubectl` with permission to manage namespaces and Deployments.
- Go **1.20 or newer**, as declared in [`go.mod`](./go.mod).
- [krew](https://krew.sigs.k8s.io/docs/user-guide/setup/install/) is optional if installing directly from source. This repository does not include a published krew plugin manifest.

The old prerequisite “Kubernetes >= v1.23.0” is not enforced in the plugin source; check compatibility with your cluster separately.

## Build and install

From the repository root:

```bash
cd platforms/krew
make deploy
```

The [Makefile](./Makefile) builds `bin/kubectl-bilipro` and uses `sudo install` to copy it alongside the `kubectl` binary. Check that directory and your privileges before running the install target. Alternatively, `make build` produces the binary in `bin/` for you to install in a directory on `PATH`.

## Deploy or update

Use `init` again to apply updated deployment configuration:

```bash
kubectl bilipro init --config config.yaml
```

The `--config` file is a YAML list of Kubernetes environment-variable objects, like the bundled [config.yaml](./config.yaml):

```yaml
- name: Ray_BiliBiliCookies__1
  value: "your Cookie"
- name: Ray_DailyTaskConfig__Cron
  value: "11 11 * * *"
```

Keep credentials in that file private. The old guide called the cron variable required; the plugin only reads and adds the provided list and does not validate these particular keys. Its defaults and options are:

| Option | Behavior |
| --- | --- |
| `--config=config.yaml` | Path to the environment-variable list; supply a real file. |
| `--image=zai7lou/bilibili_tool_pro:2.0.1` | Override the legacy default image. A different image may not support these commands. |
| `--namespace=bilipro` | Target namespace (default `bilipro`). |
| `--image-pull-secret=<secret-name>` | Optional Kubernetes image pull secret. |
| `--login` | After deployment, attempt QR login by executing the legacy console DLL in a pod. |
| `--output` | Print generated YAML; **not** a safe dry run: the implementation still executes `kubectl apply`. |

The generated Deployment defaults to the `bilipro` namespace. Avoid `default` and `kube-*` namespaces for this plugin: its delete command unconditionally attempts to delete the namespace too.

## Inspect, delete, and version

```bash
kubectl bilipro get --namespace=bilipro --name=bilibilipro
kubectl bilipro version
```

**Deletion warning:** `kubectl bilipro delete --namespace=bilipro --name=bilibilipro` runs `kubectl delete -f -` for the bundled Deployment and then `kubectl delete ns bilipro`. It does not honor `--name` when constructing the manifest and can delete other workloads when removing the namespace. Inspect the namespace and its resources before using this command; for a shared namespace, use `kubectl delete deployment bilibilipro -n bilipro` instead and leave the namespace intact.

## Packaging for krew

See [krew's packaging instructions](https://krew.sigs.k8s.io/docs) if you want to package and publish the built plugin yourself; `make deploy` installs locally and does not publish it to the krew index.
