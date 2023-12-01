package cmd

import (
	"fmt"
	"io"
	"os/exec"
	"strings"

	"sigs.k8s.io/kustomize/api/krusty"
	"sigs.k8s.io/kustomize/api/types"
	"sigs.k8s.io/yaml"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/options"
	helper "github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/utils"
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
				fmt.Println(err)
				return err
			}
			fmt.Println("bilibili tool is removed from your cluster")
			return nil
		},
	}

	f := cmd.Flags()
	f.StringVarP(&o.deployOpts.Namespace, "namespace", "n", "bilipro", "namespace scope for this request")
	f.StringVarP(&o.deployOpts.Name, "name", "", "bilibilipro", "name of deployment to delete")
	return cmd
}

func (o *deleteCmd) run(writer io.Writer) error {
	inDiskSys, err := helper.GetResourceFileSys()
	if err != nil {
		return err
	}

	// remove namespace files lastly
	resources := []string{"base/bilibiliPro/deployment.yaml"}

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
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
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
		return helper.GenErrorMsg(helper.TEMPLATE_ERROR, err.Error())
	}

	// do kubectl delete
	cmd := exec.Command("kubectl", "delete", "-f", "-")

	if err := helper.Run(cmd, strings.NewReader(string(yml))); err != nil {
		return err
	}

	// delete the namespace
	cmd = exec.Command("kubectl", "delete", "ns", o.deployOpts.Namespace)
	if err := helper.Run(cmd, nil); err != nil {
		return err
	}
	return nil
}
