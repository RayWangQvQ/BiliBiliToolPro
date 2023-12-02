package main

import (
	"fmt"
	"os"

	"github.com/spf13/pflag"

	"github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/cmd"
	helper "github.com/RayWangQvQ/BiliBiliToolPro/krew/pkg/utils"
	"k8s.io/cli-runtime/pkg/genericclioptions"
)

func main() {
	flags := pflag.NewFlagSet("kubectl-bilipro", pflag.ExitOnError)
	pflag.CommandLine = flags

	cmd := cmd.NewExecutor(genericclioptions.IOStreams{In: os.Stdin, Out: os.Stdout, ErrOut: os.Stderr})
	if err := cmd.Execute(); err != nil {
		fmt.Println(helper.GenErrorMsg(helper.SERVER_ERROR, err.Error()).Error())
		os.Exit(1)
	}
}
