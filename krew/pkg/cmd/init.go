package cmd

import (
	"bufio"
	"encoding/json"
	"fmt"
	"io"
	"io/ioutil"
	"os"
	"os/exec"
	"strings"

	corev1 "k8s.io/api/core/v1"

	"sigs.k8s.io/kustomize/kyaml/resid"

	"sigs.k8s.io/yaml"

	"sigs.k8s.io/kustomize/api/types"

	"k8s.io/klog/v2"

	"github.com/spf13/cobra"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/options"
	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/utils"
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
				klog.Warning(err)
				return err
			}
			return nil
		},
	}

	f := cmd.Flags()
	f.StringVarP(&o.deployOpts.Image, "image", "i", "zai7lou/bilibili_tool_pro:0.2.1", "bilibilipro image")
	f.StringVarP(&o.deployOpts.Namespace, "namespace", "n", "bilipro", "namespace scope for this request")
	f.StringVar(&o.deployOpts.ImagePullSecret, "image-pull-secret", "", "image pull secret to be used for pulling bilibilipro image")
	f.StringVarP(&o.deployOpts.ConfigFilePath, "config", "c", "", "the config file contanis the environment variables")
	f.BoolVarP(&o.output, "output", "o", false, "dry run this command and generate requisite yaml")
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
	inDiskSys, err := utils.GetResourceFileSys()
	if err != nil {
		return err
	}

	// if the bilibili tool is deployed under system/pre-defined namespace, ignore the namespace file
	resources := []string{}
	if o.deployOpts.Namespace == "default" || o.deployOpts.Namespace == "kube-system" || o.deployOpts.Namespace == "kube-public" {
		resources = []string{"bilipro/bilibiliPro"}
	} else {
		resources = []string{"bilipro/ns", "bilipro/bilibiliPro"}
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
	content, err := ioutil.ReadFile(o.deployOpts.ConfigFilePath)
	if err != nil {
		klog.Error(err)
		return err
	}

	envs := []normalEnvVars{}
	err = yaml.Unmarshal(content, &envs)
	if err != nil {
		klog.Error(err)
		return err
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
					Name: "bilibilipro",
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
		klog.Error(err)
		return err
	}

	kustFile, err := inDiskSys.Create("kustomization.yaml")
	if err != nil {
		klog.Error(err)
		return err
	}

	_, err = kustFile.Write(kustYaml)
	if err != nil {
		klog.Error(err)
		return err
	}

	// kustomize build the target location
	k := krusty.MakeKustomizer(
		krusty.MakeDefaultOptions(),
	)

	m, err := k.Run(inDiskSys, "./bilipro")
	if err != nil {
		klog.Error(err)
		return err
	}

	yml, err := m.AsYaml()
	if err != nil {
		klog.Error(err)
		return err
	}

	if o.output {
		_, err = writer.Write(yml)
		klog.Error(err)
		return err
	}

	// do kubectl apply
	cmd := exec.Command("kubectl", "apply", "-f", "-")

	cmd.Stdin = strings.NewReader(string(yml))

	stdoutReader, _ := cmd.StdoutPipe()
	stdoutScanner := bufio.NewScanner(stdoutReader)
	go func() {
		for stdoutScanner.Scan() {
			fmt.Println(stdoutScanner.Text())
		}
	}()
	stderrReader, _ := cmd.StderrPipe()
	stderrScanner := bufio.NewScanner(stderrReader)
	go func() {
		for stderrScanner.Scan() {
			fmt.Println(stderrScanner.Text())
		}
	}()
	err = cmd.Start()
	if err != nil {
		fmt.Printf("Error : %v \n", err)
		os.Exit(1)
	}

	// Stuck here until there are out and err
	err = cmd.Wait()
	if err != nil {
		fmt.Printf("Error: %v \n", err)
		os.Exit(1)
	}

	return nil
}

func (o *initCmd) serializeJSONPatchOps(jp []interface{}) string {
	jpJSON, _ := json.Marshal(jp)
	return string(jpJSON)
}
