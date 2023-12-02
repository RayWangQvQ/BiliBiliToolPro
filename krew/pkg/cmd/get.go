package cmd

import (
	"fmt"
	"io"
	"os/exec"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/options"
	helper "github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/utils"
	"github.com/spf13/cobra"
)

const (
	getDesc = `
'get' command get bilibilipro tool deployment.`
	getExample = `  kubectl bilipro get <--name deployment_name --namespace namespace_name>`
)

type getCmd struct {
	out    io.Writer
	errOut io.Writer

	deployOpts options.DeployOptions
}

func newGetCmd(out io.Writer, errOut io.Writer) *cobra.Command {
	o := &getCmd{out: out, errOut: errOut}

	cmd := &cobra.Command{
		Use:     "get",
		Short:   "Get bilibilipro",
		Long:    getDesc,
		Example: getExample,
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
	f.StringVarP(&o.deployOpts.Namespace, "namespace", "n", "bilipro", "namespace scope for this request")
	f.StringVarP(&o.deployOpts.Name, "name", "", "bilibilipro", "name of deployment to get")
	return cmd
}

func (o *getCmd) run(writer io.Writer) error {
	// do kubectl get
	cmd := exec.Command("kubectl", "get", "deploy", o.deployOpts.Name, "-n", o.deployOpts.Namespace)

	if err := helper.Run(cmd, nil); err != nil {
		return err
	}

	return nil
}
