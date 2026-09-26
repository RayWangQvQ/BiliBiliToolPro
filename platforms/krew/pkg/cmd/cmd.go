package cmd

import (
	"log"

	"github.com/spf13/cobra"
	"k8s.io/cli-runtime/pkg/genericclioptions"
)

const (
	biliproDesc = `Manage and deploy bilibili pro tools on k8s`
	kubeconfig  = "kubeconfig"
)

var (
	confPath string
	rootCmd  = &cobra.Command{
		Use:          "bilipro",
		Long:         biliproDesc,
		SilenceUsage: true,
	}
)

func init() {
	rootCmd.PersistentFlags().StringVar(&confPath, kubeconfig, "", "Custom kubeconfig path")

	log.SetFlags(log.Ldate | log.Ltime | log.Lshortfile)
}

// New creates a new root command for kubectl-bilipro
func NewExecutor(streams genericclioptions.IOStreams) *cobra.Command {
	cobra.EnableCommandSorting = false
	rootCmd.AddCommand(newInitCmd(rootCmd.OutOrStdout(), rootCmd.ErrOrStderr()))
	// If you want to update, just init again
	rootCmd.AddCommand(newGetCmd(rootCmd.OutOrStdout(), rootCmd.ErrOrStderr()))
	rootCmd.AddCommand(newDeleteCmd(rootCmd.OutOrStdout(), rootCmd.ErrOrStderr()))
	rootCmd.AddCommand(newVersionCmd(rootCmd.OutOrStdout(), rootCmd.ErrOrStderr()))
	return rootCmd
}
