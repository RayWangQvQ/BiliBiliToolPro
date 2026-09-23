package options

// DeployOptions encapsulates the CLI options for a BiliBiliPro
type DeployOptions struct {
	Name            string
	Image           string
	Namespace       string
	ImagePullSecret string
	ConfigFilePath  string
}
