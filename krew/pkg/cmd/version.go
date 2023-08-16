package cmd

import (
	"fmt"
	"io"

	"github.com/spf13/cobra"
)

// version provides the version of this plugin
var version = "NO.VERSION"

const (
	versionDesc = `
'version' command displays the kubectl plugin version.`
	versionExample = `  kubectl bilipro version`
)

type versionCmd struct {
	out    io.Writer
	errOut io.Writer
}

func newVersionCmd(out io.Writer, errOut io.Writer) *cobra.Command {
	o := &versionCmd{out: out, errOut: errOut}

	cmd := &cobra.Command{
		Use:     "version",
		Short:   "Display plugin version",
		Long:    versionDesc,
		Example: versionExample,
		Args:    cobra.MaximumNArgs(0),
		RunE: func(cmd *cobra.Command, args []string) error {
			err := o.run()
			if err != nil {
				fmt.Println(err)
				return err
			}
			return nil
		},
	}

	return cmd
}

// run initializes local config and installs BilibiliPro Plugin to Kubernetes cluster.
func (o *versionCmd) run() error {
	fmt.Println(version)
	return nil
}
