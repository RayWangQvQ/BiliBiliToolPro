package cmd

import (
	"encoding/json"
	"fmt"
	"io"
	"os"
	"os/exec"
	"strings"

	corev1 "k8s.io/api/core/v1"

	"sigs.k8s.io/kustomize/kyaml/resid"

	"sigs.k8s.io/yaml"

	"sigs.k8s.io/kustomize/api/types"

	"github.com/spf13/cobra"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/options"
	helper "github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/utils"
	"sigs.k8s.io/kustomize/api/krusty"
)

const (
	initDesc = `
 'init' command creates BilibiliPro deployment along with all the dependencies.`
	initExample = `  kubectl bilipro init --config <config-file>`
)

type initCmd struct {
	out        io.Writer
	errOut     io.Writer
	output     bool
	login      bool
	deployOpts options.DeployOptions
}

func newInitCmd(out io.Writer, errOut io.Writer) *cobra.Command {
	o := &initCmd{out: out, errOut: errOut}

	cmd := &cobra.Command{
		Use:     "init",
		Short:   "Initialize bilipro",
		Long:    initDesc,
		Example: initExample,
		Args:    cobra.MaximumNArgs(0),
		RunE: func(cmd *cobra.Command, args []string) error {
			err := o.run(out)
			if err != nil {
				fmt.Println(err)
				return err
			}
			return nil
		},
	}

	f := cmd.Flags()
	f.StringVarP(&o.deployOpts.Image, "image", "i", "zai7lou/bilibili_tool_pro:2.0.1", "bilibilipro image")
	f.StringVarP(&o.deployOpts.Namespace, "namespace", "n", "bilipro", "namespace scope for this request")
	f.StringVar(&o.deployOpts.ImagePullSecret, "image-pull-secret", "", "image pull secret to be used for pulling bilibilipro image")
	f.StringVarP(&o.deployOpts.ConfigFilePath, "config", "c", "", "the config file contanis the environment variables")
	f.BoolVarP(&o.output, "output", "o", false, "dry run this command and generate requisite yaml")
	f.BoolVarP(&o.login, "login", "l", false, "scan QR login code")
	return cmd
}

type opStr struct {
	Op    string `json:"op"`
	Path  string `json:"path"`
	Value string `json:"value"`
}

type opInterface struct {
	Op    string      `json:"op"`
	Path  string      `json:"path"`
	Value interface{} `json:"value"`
}

type normalEnvVars struct {
	Name  string `json:"name"`
	Value string `json:"value"`
}

// run initializes local config and installs BiliBiliPro tool to Kubernetes Cluster.
func (o *initCmd) run(writer io.Writer) error {
	inDiskSys, err := helper.GetResourceFileSys()
	if err != nil {
		return err
	}

	// TODO: All about paths are a little bit tricky should give it more thoughts

	fmt.Println("Creating the kustomization file")
	// if the bilibili tool is deployed under system/pre-defined namespace, ignore the namespace file
	var resources []string // nolint: go-staticcheck
	if o.deployOpts.Namespace == "default" || o.deployOpts.Namespace == "kube-system" || o.deployOpts.Namespace == "kube-public" {
		resources = []string{"base/bilibiliPro/deployment.yaml"}
	} else {
		resources = []string{"base/ns/namespace.yaml", "base/bilibiliPro/deployment.yaml"}
	}

	// write the kustomization file
	kustomizationYaml := types.Kustomization{
		TypeMeta: types.TypeMeta{
			Kind:       "Kustomization",
			APIVersion: "kustomize.config.k8s.io/v1beta1",
		},
		Resources:       resources,
		PatchesJson6902: []types.Patch{},
	}

	var deployDepPatches []interface{}
	// create patches for the supplied arguments
	if o.deployOpts.Image != "" {
		deployDepPatches = append(deployDepPatches, opStr{
			Op:    "replace",
			Path:  "/spec/template/spec/containers/0/image",
			Value: o.deployOpts.Image,
		})
	}
	// create patches for the env
	content, err := os.ReadFile(o.deployOpts.ConfigFilePath)
	if err != nil {
		return helper.GenErrorMsg(helper.FILE_ERROR, err.Error())
	}

	envs := []normalEnvVars{}
	err = yaml.Unmarshal(content, &envs)
	if err != nil {
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
	}

	deployDepPatches = append(deployDepPatches, opInterface{
		Op:    "add",
		Path:  "/spec/template/spec/containers/0/env",
		Value: envs,
	})

	if o.deployOpts.ImagePullSecret != "" {
		deployDepPatches = append(deployDepPatches, opInterface{
			Op:    "add",
			Path:  "/spec/template/spec/imagePullSecrets",
			Value: []corev1.LocalObjectReference{{Name: o.deployOpts.ImagePullSecret}},
		})
	}

	// attach the patches to the kustomization file
	if len(deployDepPatches) > 0 {
		kustomizationYaml.PatchesJson6902 = append(kustomizationYaml.PatchesJson6902, types.Patch{
			Patch: o.serializeJSONPatchOps(deployDepPatches),
			Target: &types.Selector{
				ResId: resid.ResId{
					Gvk: resid.Gvk{
						Group:   "apps",
						Version: "v1",
						Kind:    "Deployment",
					},
					Name:      "bilibilipro",
					Namespace: o.deployOpts.Namespace,
				},
			},
		})
	}

	// Not deploying in kube-* namespace
	if o.deployOpts.Namespace == "kube-system" || o.deployOpts.Namespace == "kube-public" {
		fmt.Println("better not deployed under system namesapce")
	}

	if o.deployOpts.Namespace != "" {
		kustomizationYaml.Namespace = o.deployOpts.Namespace
	}
	// Compile the kustomization to a file and create on the in memory filesystem
	kustYaml, err := yaml.Marshal(kustomizationYaml)
	if err != nil {
		return err
	}

	kustFile, err := inDiskSys.Create("./bilipro/kustomization.yaml")
	if err != nil {
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
	}

	_, err = kustFile.Write(kustYaml)
	if err != nil {
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
	}

	// kustomize build the target location
	k := krusty.MakeKustomizer(
		krusty.MakeDefaultOptions(),
	)

	m, err := k.Run(inDiskSys, "./bilipro")
	if err != nil {
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
	}

	yml, err := m.AsYaml()
	if err != nil {
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
	}

	if o.output {
		_, err = writer.Write(yml)
		if err != nil {
			return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
		}
	}

	fmt.Println("Applying the kustomization file")
	// do kubectl apply
	// make sure kubectl is under your PATH
	cmd := exec.Command("kubectl", "apply", "-f", "-")

	if err := helper.Run(cmd, strings.NewReader(string(yml))); err != nil {
		return err
	}

	// if there is login required, exectue the login command as the last step
	if o.login {
		fmt.Println("please login...")
		client, _, err := helper.GetK8sClient()
		if err != nil {
			return err
		}

		// get the pod name
		podName, err := helper.GetBiliName(client, o.deployOpts.Namespace, "bilibilipro")
		if err != nil {
			return err
		}

		fmt.Println("wait for the deployment to be ready")
		// Wait for the deployment ready
		checkCmdArgs := []string{
			"rollout",
			"status",
			"deployment/bilibilipro",
			"-n",
			o.deployOpts.Namespace,
		}
		checkCmd := exec.Command("kubectl", checkCmdArgs...)

		for {
			if err := checkCmd.Start(); err != nil {
				fmt.Printf("deployment is not ready yet, current status: %v\n", err)
				continue
			}

			err := checkCmd.Wait()
			if err == nil {
				fmt.Printf("deployment is ready\n")
				break
			}
			fmt.Printf("deployment is not ready yet, current status: %v\n", err)
		}

		fmt.Println("please scan the QR code")
		// Exec the login command
		args := []string{
			"exec",
			podName,
			"-n",
			o.deployOpts.Namespace,
			"--",
			"dotnet",
			"Ray.BiliBiliTool.Console.dll",
			"--runTasks=Login",
		}
		cmd := exec.Command("kubectl", args...)

		if err := helper.Run(cmd, nil); err != nil {
			return err
		}
	}

	return nil
}

func (o *initCmd) serializeJSONPatchOps(jp []interface{}) string {
	jpJSON, _ := json.Marshal(jp)
	return string(jpJSON)
}
