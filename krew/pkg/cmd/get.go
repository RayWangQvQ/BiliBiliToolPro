package cmd

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"os/exec"

	"k8s.io/klog/v2"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/options"
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
				klog.Warning(err)
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

	err := cmd.Start()
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
