package cmd

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"os/exec"
	"strings"

	"sigs.k8s.io/kustomize/api/krusty"
	"sigs.k8s.io/kustomize/api/types"
	"sigs.k8s.io/yaml"

	"k8s.io/klog/v2"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/options"
	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/utils"
	"github.com/spf13/cobra"
)

const (
	deleteDesc = `
'delete' command delete bilibilipro tool.`
	deleteExample = `  kubectl bilipro delete <--name deployment_name>`
)

type deleteCmd struct {
	out        io.Writer
	errOut     io.Writer
	output     bool
	deployOpts options.DeployOptions
}

func newDeleteCmd(out io.Writer, errOut io.Writer) *cobra.Command {
	o := &deleteCmd{out: out, errOut: errOut}

	cmd := &cobra.Command{
		Use:     "delete",
		Short:   "Delete bilibilipro",
		Long:    deleteDesc,
		Example: deleteExample,
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
	f.StringVarP(&o.deployOpts.Namespace, "namespace", "n", "bilipro", "namespace scope for this request")
	f.StringVarP(&o.deployOpts.Name, "name", "", "bilibilipro", "name of deployment to delete")
	return cmd
}

func (o *deleteCmd) run(writer io.Writer) error {
	inDiskSys, err := utils.GetResourceFileSys()
	if err != nil {
		klog.Error(err)
		return err
	}

	// if the bilibili tool is deployed under system/pre-defined namespace, ignore the namespace file
	resources := []string{}
	if o.deployOpts.Namespace == "default" || o.deployOpts.Namespace == "kube-system" || o.deployOpts.Namespace == "kube-public" {
		resources = []string{"bilipro/bilibiliPro"}
	} else {
		resources = []string{"bilipro/bilibiliPro", "bilipro/ns"}
	}

	// write the kustomization file

	kustomizationYaml := types.Kustomization{
		TypeMeta: types.TypeMeta{
			Kind:       "Kustomization",
			APIVersion: "kustomize.config.k8s.io/v1beta1",
		},
		Resources: resources,
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

	// do kubectl delete
	cmd := exec.Command("kubectl", "delete", "-f", "-")

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

	// stuck here to wait for the completion
	err = cmd.Wait()
	if err != nil {
		fmt.Printf("Error: %v \n", err)
		os.Exit(1)
	}

	return nil
}
